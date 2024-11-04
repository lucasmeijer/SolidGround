using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Tests;

public class ExperimentTests : IntegrationTestBase
{
    [Fact]
    public async Task GetNonExistingInput_Returns_404()
    {
        new WebApplicationFactory<TestClient>();
    }


    class TestClient
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var app = builder.Build();
            app.MapGet("/", () => "Hello World!");
            app.Run();
        }
    }
    
}