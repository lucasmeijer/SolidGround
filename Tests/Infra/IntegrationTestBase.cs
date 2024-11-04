using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SolidGround;

public abstract class IntegrationTestBase : IDisposable
{
    protected HttpClient Client { get; }
    WebApplicationFactory<Program> Factory { get; }
    IServiceScope Scope { get; } 
    protected AppDbContext DbContext { get; }
    
    protected IntegrationTestBase()
    {
        var databaseName = Guid.NewGuid().ToString();
        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(databaseName: databaseName);
                });
            });
        });
        var baseAddress = Factory.Server.BaseAddress;
        Client = Factory.CreateClient();
        Scope = Factory.Services.CreateScope();
        DbContext = Scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }
    
    void IDisposable.Dispose() => Scope.Dispose();
}
