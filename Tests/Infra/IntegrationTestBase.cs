using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SolidGround;
using Xunit;

public abstract class IntegrationTestBase : IClassFixture<WebApplicationFactory<Program>> , IDisposable
{
    protected HttpClient Client { get; }
    WebApplicationFactory<Program> Factory { get; }
    IServiceScope Scope { get; } 
    protected AppDbContext DbContext { get; }
    
    protected IntegrationTestBase(WebApplicationFactory<Program> factory)
    {
        string dbName = Guid.NewGuid().ToString();
        
        Factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(databaseName: dbName);
                });
            });
        });

        Client = Factory.CreateClient();
        Scope = Factory.Services.CreateScope();
        DbContext = Scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }
    
    void IDisposable.Dispose() => Scope.Dispose();
}