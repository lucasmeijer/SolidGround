using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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
    public enum TestEnum
    {
        One,
        Two,
        Three
    }
    
    class TestVariables : SolidGroundVariables
    {
        public override string? PromptingGuidelines => "myguidelines";
        public override string? ApplicationInformation => "myappinformation";
        
        public string Prompt { get; set; } = "Tell me a joke about a";
        public TestEnum TestEnum { get; set; }
        public bool Bool { get; set; }
    }

    [Fact]
    public async Task SolidGroundEndPointReturnsVariables()
    {
        var webAppFactory = await SetupHorseJokeClientApp();
        var client = webAppFactory.HttpClient;

        var cif = await ExecutionsEndPoints.ClientInfoFor(null, client, true);

        var expected = new SolidGroundRouteInfo
        {
            Route = "/joke",
            ApplicationInformation = "myappinformation",
            PromptingGuidelines = "myguidelines",
            StringVariables =
            [
                new()
                {
                    Name = "Prompt",
                    Value = "Tell me a joke about a",
                    Options = null
                },
                new()
                {
                    Name = "TestEnum",
                    Value = "One",
                    Options = ["One", "Two", "Three"]
                },
                new()
                {
                    Name = "Bool",
                    Value = "false",
                    Options = ["true", "false"]
                }
            ],
            EvaluationCriteria =
            [
            ]
        };

        Assert.Equivalent(expected, cif);
    }
    
    [Fact]
    public async Task CapturedSessionGetsUploaded()
    {
        var solidGroundConsumingApp = await SetupHorseJokeClientApp();

        var response = await solidGroundConsumingApp.HttpClient.GetAsync("/joke?subject=horse");
        var s = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        Assert.Equal("Tell me a joke about a horse", await response.Content.ReadFromJsonAsync<string>());
        
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
        
        var outputStringVariable = output.StringVariables.Single(v => v.Name == "Prompt");
        Assert.Equal("Prompt", outputStringVariable.Name);
        Assert.Equal("Tell me a joke about a", outputStringVariable.Value);
    
        var executionStringVariable = execution.StringVariables.Single(v => v.Name == "Prompt");
        Assert.Equal("Prompt", executionStringVariable.Name);
        Assert.Equal("Tell me a joke about a", executionStringVariable.Value);
    }

    async Task<WebApplicationUnderTest<int>> SetupHorseJokeClientApp(Func<SolidGroundSessionAccessor, string, Task<IResult>>? impl = null) =>
        await SetupTestWebApplicationFactory(endpointBuilder =>
        {
            endpointBuilder.MapGet("/joke", (string subject, SolidGroundSessionAccessor accessor) => (impl ?? DefaultJokeImplementation)(accessor, subject))
                .ExposeToSolidGround<TestVariables>();
        });

    static async Task<IResult> DefaultJokeImplementation(SolidGroundSessionAccessor accessor, string subject)
    {
        var session = accessor.Session ?? throw new ArgumentException("asd");
        var joke = session.GetVariables<TestVariables>().Prompt + " " + subject;
        session.AddResult(joke);

        await session.CompleteAsync(true);
        return Results.Ok(joke);
    }
    
    [Fact]
    public async Task CanPostExecution()
    {
        await using var consumingAppClient = await SetupClientAppAndTriggerFirstInput(null);

        var execution = await PostExecutionAndWaitUntilFinished(new()
        {
            BaseUrl = consumingAppClient.HttpClient.BaseAddress?.ToString() ?? throw new Exception("No baseaddress"),
            Inputs = [DbContext.Inputs.Single().Id],
            StringVariables = [new() { Name = "Prompt", Value = "Give me a haiku about", Options = []}],
            RunAmount = 1
        });

        var output = execution.Outputs.Single();
        Assert.Equal(ExecutionStatus.Completed, output.Status);
        
        var component = output.Components.Single();
        Assert.Equal("result", component.Name);
        Assert.Equal("Give me a haiku about horse", component.Value);
    }

    async Task<WebApplicationUnderTest<int>> SetupClientAppAndTriggerFirstInput(Func<SolidGroundSessionAccessor, string, Task<IResult>>? impl)
    {
        var solidGroundConsumingApp = await SetupHorseJokeClientApp(impl);
        var consumingAppClient = solidGroundConsumingApp.HttpClient;
        var response = await consumingAppClient.GetAsync("/joke?subject=horse");
        response.EnsureSuccessStatusCode();
        Assert.Equal("Tell me a joke about a horse", await response.Content.ReadFromJsonAsync<string>());
        await solidGroundConsumingApp.Services.GetRequiredService<SolidGroundBackgroundService>().FlushAsync();
        await AssertDatabaseAfterInitialInputSubmitted();
        return solidGroundConsumingApp;
    }

    [Fact]
    public async Task PostExecution_Against_BrokenApp()
    {
        bool firstRequest = true;
        Func<SolidGroundSessionAccessor,string,Task<IResult>> impl = (accessor, s) =>
        {
            if (firstRequest)
            {
                firstRequest = false;
                return DefaultJokeImplementation(accessor, s);
            }
            
            return Task.FromResult(Results.InternalServerError("Hair on fire"));
        };

        await using var clientApp = await SetupClientAppAndTriggerFirstInput(impl);
        
        var execution = await PostExecutionAndWaitUntilFinished(new()
        {
            BaseUrl = clientApp.HttpClient.BaseAddress?.ToString() ?? throw new Exception("No baseaddress"),
            Inputs = [DbContext.Inputs.Single().Id],
            StringVariables = [new() { Name = "Prompt", Value = "Give me a haiku about", Options = []}],
            RunAmount = 1
        });
        var output = execution.Outputs.Single();

        Assert.Equal(ExecutionStatus.Failed, output.Status);
        var component = output.Components.Single();
        Assert.Equal("Http Error InternalServerError", component.Name);
        Assert.Equal("body: \"Hair on fire\"", component.Value);
    }

    async Task<Execution> PostExecutionAndWaitUntilFinished(RunExecutionDto runExecutionDto)
    {
        var response = await Client.PostAsJsonAsync("/api/executions", runExecutionDto);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        
        var executionUrl = response.Headers.Location ?? throw new ApplicationException("No location found on created execution");
        while (true)
        {
            var executionStatus = await Client.GetFromJsonAsync<ExecutionStatusDto>(executionUrl) ?? throw new InvalidOperationException();
            if (executionStatus.Finished)
                break;
            await Task.Delay(100);
        }
    
        var id = int.Parse(executionUrl.ToString().Split("/").Last());
        return await DbContext.Executions
                   .Include(e => e.Outputs)
                   .ThenInclude(o => o.Components)
                   .FirstOrDefaultAsync(e => e.Id == id)
               ?? throw new Exception();
    }


    Task<WebApplicationUnderTest<int>> SetupTestWebApplicationFactory(Action<IEndpointRouteBuilder>? addEndPoints)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSolidGround(_ => Tenant.ApiKey);
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