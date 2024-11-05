using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SolidGround;

public class ServerUnderTest_TestServer : IAsyncDisposable
{
    WebApplicationFactory<Program> Factory { get; }
    public HttpClient Client { get; }
    public AppDbContext DbContext { get; }
    IServiceScope Scope { get; }
    public ServerUnderTest_TestServer()
    {
        string databaseName = Guid.NewGuid().ToString();
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
        
        Client = Factory.CreateClient();
        Scope = Factory.Services.CreateScope();
        DbContext = Scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    public async ValueTask DisposeAsync()
    {
        await DbContext.DisposeAsync();
        Scope.Dispose();
        await Factory.DisposeAsync();
    }
}