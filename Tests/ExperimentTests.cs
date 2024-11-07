using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SolidGround;
using SolidGroundClient;
using Xunit;

namespace Tests;

public class ExecutionEndpointTests : IntegrationTestBase
{
    class TestVariables : SolidGroundVariables
    {
        public SolidGroundVariable<string> Prompt = new("Prompt", "Tell me a joke about a");
    }

    [Fact]
    public async Task SolidGroundEndPointReturnsVariables()
    {
        var webAppFactory = await SetupTestWebApplicationFactory(null);
        var client = webAppFactory.HttpClient;
        var response = await client.GetAsync(SolidGroundExtensions.EndPointRoute);
        response.EnsureSuccessStatusCode();
        var s = await response.Content.ReadFromJsonAsync<AvailableVariablesDto>();
        Assert.Equivalent(s, new AvailableVariablesDto() { StringVariables = [ new() { Name = "Prompt", Value = "Tell me a joke about a"}]});
    }
    
    [Fact]
    public async Task CapturedSessionGetsUploaded()
    {
        var solidGroundConsumingApp = await SetupTestWebApplicationFactory(endpointBuilder =>
        {
            endpointBuilder.MapGet("/joke", async (string subject, SolidGroundSession session, TestVariables testVariables) =>
            {
                await session.CaptureRequestAsync();

                var joke =  testVariables.Prompt.Value + " " + subject;
                session.AddResult(joke);
                return joke;
            });
        });

        var response = await solidGroundConsumingApp.HttpClient.GetAsync("/joke?subject=horse");
        response.EnsureSuccessStatusCode();
        Assert.Equal("Tell me a joke about a horse", await response.Content.ReadAsStringAsync());
        
        await solidGroundConsumingApp.Services.GetRequiredService<SolidGroundBackgroundService>().FlushAsync();
        
        var execution = await DbContext.Executions
            .Include(e=>e.Outputs)
            .ThenInclude(o => o.Input)
            .Include(e=>e.Outputs)
            .ThenInclude(o => o.Components)
            .Include(e=>e.Outputs)
            .ThenInclude(o => o.StringVariables)
            .SingleAsync();

        var output = execution.Outputs.Single();
        var component = output.Components.Single();
        Assert.Equal("result", component.Name);
        Assert.Equal("Tell me a joke about a horse", component.Value);

        var stringVariable = output.StringVariables.Single();
        Assert.Equal("Prompt", stringVariable.Name);
        Assert.Equal("Tell me a joke about a", stringVariable.Value);
    }

    record JokeJto(string Subject);
    
    [Fact]
    public async Task CanPostExecution()
    {
        var solidGroundConsumingApp = await SetupTestWebApplicationFactory(endpointBuilder =>
        {
            endpointBuilder.MapPost("/joke",
                async ([FromBody] JokeJto jokeDto, SolidGroundSession session, TestVariables testVariables) =>
                {
                    await session.CaptureRequestAsync();

                    var joke = testVariables.Prompt.Value + " " + jokeDto.Subject;
                    session.AddResult(joke);
                    return joke;
                });
        });

        var consumingAppClient = solidGroundConsumingApp.HttpClient;
        var response1 = await consumingAppClient.PostAsJsonAsync("/joke", new JokeJto("horse"));
        response1.EnsureSuccessStatusCode();
        Assert.Equal("Tell me a joke about a horse", await response1.Content.ReadAsStringAsync());
        await solidGroundConsumingApp.Services.GetRequiredService<SolidGroundBackgroundService>().FlushAsync();
        
        var response = await Client.PostAsJsonAsync("/api/executions", new RunExecutionDto()
        {
            EndPoint = consumingAppClient.BaseAddress+"joke",
            Inputs = [DbContext.Inputs.Single().Id],
            StringVariables = [new() { Name = "Prompt", Value = "Give me a haiku about"}]
        });
        Assert.Equal(StatusCodes.Status201Created, (int)response.StatusCode);
        
        var executionUrl = response.Headers.Location ?? throw new ApplicationException("No location found on created execution");
        while (true)
        {
            var executionStatus = await Client.GetFromJsonAsync<ExecutionStatusDto>(executionUrl) ?? throw new InvalidOperationException();
            if (executionStatus.Finished)
                break;
            await Task.Delay(100);
        }

        var execution = await DbContext.Executions.FindAsync(int.Parse(executionUrl.ToString().Split("/").Last()))
            ?? throw new InvalidOperationException();
        await DbContext.Entry(execution).Collection(e => e.Outputs).LoadAsync();
        var output = execution.Outputs.Single();
        await DbContext.Entry(output).Collection(e => e.Components).LoadAsync();
        var component = output.Components.Single();
        Assert.Equal("result", component.Name);
        Assert.Equal("Give me a haiku about horse", component.Value);
    }
    
    Task<WebApplicationUnderTest<int>> SetupTestWebApplicationFactory(Action<IEndpointRouteBuilder>? addEndPoints)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSolidGround<TestVariables>();
        builder.Services.AddRouting();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            { SolidGroundConstants.SolidGroundBaseUrl, Client.BaseAddress!.ToString() },
        }!);
        var app = builder.Build();
        app.UseRouting();
        app.MapSolidGroundEndpoint();
        addEndPoints?.Invoke(app);
        
        return WebApplicationUnderTest<int>.StartAsync(app);
    }
}