using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SolidGround;
using SolidGroundClient;
using TurboFrames;
using Xunit;

namespace Tests;

public class ExecutionEndpointTests : IntegrationTestBase
{
    class TestVariables : SolidGroundVariables
    {
        public string Prompt { get; set; } = "Tell me a joke about a";
    }

    [Fact]
    public async Task SolidGroundEndPointReturnsVariables()
    {
        var webAppFactory = await SetupHorseJokeClientApp();
        var client = webAppFactory.HttpClient;
        var response = await client.GetAsync(SolidGroundExtensions.EndPointRoute);
        response.EnsureSuccessStatusCode();
        var s2 = await response.Content.ReadAsStringAsync();
        var jdoc = JsonDocument.Parse(s2);

        var expected = new JsonArray()
        {
            new JsonObject()
            {
                ["route"] = "/joke",
                ["variables"] = new JsonObject
                {
                    ["Prompt"] = "Tell me a joke about a"
                }
            }
        };
        
        Assert.Equal(JsonSerializer.Serialize(expected), JsonSerializer.Serialize(jdoc.RootElement));
    }
    
    [Fact]
    public async Task CapturedSessionGetsUploaded()
    {
        var solidGroundConsumingApp = await SetupHorseJokeClientApp();

        var response = await solidGroundConsumingApp.HttpClient.GetAsync("/joke?subject=horse");
        var s = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        Assert.Equal("Tell me a joke about a horse", await response.Content.ReadAsStringAsync());
        
        await solidGroundConsumingApp.Services.GetRequiredService<SolidGroundBackgroundService>().FlushAsync();
        await AssertDatabaseAfterInitialInputSubmitted();
    }
    
    async Task AssertDatabaseAfterInitialInputSubmitted()
    {
        var execution = await DbContext.Executions
            .Include(e=>e.StringVariables)
            .Include(e=>e.Outputs)
            .ThenInclude(o => o.Components)
            .Include(e=>e.Outputs)
            .ThenInclude(o => o.StringVariables)
            .SingleAsync();

        var input = DbContext.Inputs.Single();
        Assert.Equal("/joke", input.OriginalRequest_Route);
        Assert.Equal("?subject=horse", input.OriginalRequest_QueryString);
        
        var output = execution.Outputs.Single();
        var component = output.Components.Single();
        Assert.Equal("result", component.Name);
        Assert.Equal("Tell me a joke about a horse", component.Value);
        
        var outputStringVariable = output.StringVariables.Single();
        Assert.Equal("Prompt", outputStringVariable.Name);
        Assert.Equal("Tell me a joke about a", outputStringVariable.Value);
    
        //only in manually submitted executions are the stringvariables known ahead of time.
        Assert.Empty(execution.StringVariables);
    }

    async Task<WebApplicationUnderTest<int>> SetupHorseJokeClientApp()
    {
        return await SetupTestWebApplicationFactory(endpointBuilder =>
        {
            endpointBuilder.MapGet("/joke", async (string subject, SolidGroundSession session) =>
            {
                var joke = session.GetVariables<TestVariables>().Prompt + " " + subject;
                session.AddResult(joke);

                await session.CompleteAsync(true);
                return joke;
            }).ExposeToSolidGround<TestVariables>();
        });
    }
    
    [Fact]
    public async Task CanPostExecution()
    {
        var solidGroundConsumingApp = await SetupHorseJokeClientApp();

        var consumingAppClient = solidGroundConsumingApp.HttpClient;
        
        var response2 = await solidGroundConsumingApp.HttpClient.GetAsync("/joke?subject=horse");
        response2.EnsureSuccessStatusCode();
        Assert.Equal("Tell me a joke about a horse", await response2.Content.ReadAsStringAsync());
        
        await solidGroundConsumingApp.Services.GetRequiredService<SolidGroundBackgroundService>().FlushAsync();
        await AssertDatabaseAfterInitialInputSubmitted();
        
        var response = await Client.PostAsJsonAsync("/api/executions", new RunExecutionDto()
        {
            BaseUrl = consumingAppClient.BaseAddress?.ToString() ?? throw new Exception("No baseaddress"),
            Inputs = [DbContext.Inputs.Single().Id],
            StringVariables = [new() { Name = "Prompt", Value = "Give me a haiku about"}]
        });
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        
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
        builder.Services.AddSolidGround();
        builder.Services.AddRouting();
        
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            { SolidGroundConstants.SolidGroundBaseUrl, Client.BaseAddress!.ToString() },
            { SolidGroundConstants.SolidGroundApiKey, FlashCardsTenant._ApiKey },
        }!);
        var app = builder.Build();
        app.UseRouting();
        app.MapSolidGroundEndpoint();
        addEndPoints?.Invoke(app);
        
        return WebApplicationUnderTest<int>.StartAsync(app);
    }
}