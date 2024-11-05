using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SolidGround;
using SolidGroundClient;
using Xunit;

namespace Tests;


public class ExperimentTests() : IntegrationTestBase()
{
    class TestWebApplicationFactory(Action<IServiceCollection> configureServices, Action<IEndpointRouteBuilder> setupEndPoints, string solidGroundBaseURl) : WebApplicationFactory<Program>
    {
        //This line avoids the default implementation that tries to take the <Program> generic argument and create a builder through there.
        protected override IWebHostBuilder? CreateWebHostBuilder() => new WebHostBuilder();
        
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.AddRouting();
                configureServices(services);
            });

            builder.Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(setupEndPoints);
            });
            
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "SOLIDGROUND_BASE_URL", solidGroundBaseURl },
                }!);
            });
        }
    }

    class TestVariables : SolidGroundVariables
    {
        public SolidGroundVariable<string> Prompt = new("Prompt", "Tell me a joke about a");
    }

    [Fact(Skip = "wip")]
    public async Task SolidGroundEndPointReturnsVariables()
    {
        var webAppFactory = SetupTestWebApplicationFactory();
        var client = webAppFactory.CreateClient();
        var response = await client.GetAsync(SolidGroundExtensions.EndPointRoute);
        var s = await response.Content.ReadFromJsonAsync<AvailableVariablesDto>();
        Assert.Equivalent(s, new AvailableVariablesDto() { StringVariables = [ new() { Name = "Prompt", Value = "Tell me a joke about a"}]});
    }

    
    [Fact]
    public async Task CapturedSessionGetsUploaded()
    {
        var webAppFactory = SetupTestWebApplicationFactory(endpointBuilder =>
        {
            endpointBuilder.MapGet("/joke", async (string subject, SolidGroundSession session) =>
            {
                await session.CaptureRequestAsync();
                return "Hiii";
            });
        });
        
        //var client = webAppFactory.CreateDefaultClient(new Uri("https://localhost:9876"));
        
        var response = await Client.GetAsync("/joke?subject=horse");
        response.EnsureSuccessStatusCode();
        Assert.Equal("Hiii", await response.Content.ReadAsStringAsync());
        
        var execution = await DbContext.Executions
            .Include(e=>e.Outputs)
            .ThenInclude(o => o.Input)
            .SingleAsync();

        Assert.Equal(1, execution.Id);
    }
   
    
    TestWebApplicationFactory SetupTestWebApplicationFactory(Action<IEndpointRouteBuilder>? addEndPoints = null)
    {
        var baseAddress = Client.BaseAddress;
    
        return new(services => { services.AddSolidGround<TestVariables>(); }, endpointBuilder =>
        {
            endpointBuilder.MapSolidGroundEndpoint();

            addEndPoints?.Invoke(endpointBuilder);
        }, baseAddress.ToString());
    }
}