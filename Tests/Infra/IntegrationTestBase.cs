using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SolidGround;
using Xunit;

[UsedImplicitly]
class InMemoryDatabaseForTenant : IDatabaseConfigurationForTenant
{
    string _databaseName = Guid.NewGuid().ToString();
    
    public void Configure(DbContextOptionsBuilder options, Tenant? tenant)
    {
        options.UseInMemoryDatabase(databaseName: _databaseName);
    }
}

public abstract class IntegrationTestBase : IAsyncLifetime
{
    SolidGroundApplicationUnderTest WebApplicationUnderTest { get; set; } = null!;
    protected HttpClient Client => WebApplicationUnderTest.HttpClient;
    internal AppDbContext DbContext => WebApplicationUnderTest.DbContext;
    protected Tenant Tenant { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Tenant = new FlashCardsTenant();
        var webApplication = Program.CreateWebApplication<InMemoryDatabaseForTenant>([], Tenant);
        WebApplicationUnderTest = await SolidGroundApplicationUnderTest.StartAsync(webApplication, Tenant);
    }

    

    async Task IAsyncLifetime.DisposeAsync()
    {
        await WebApplicationUnderTest.DisposeAsync();
    }
}
//
// public abstract class IntegrationTestBase2 : IDisposable
// {
//     protected HttpClient Client { get; }
//     WebApplicationFactory<Program>? Factory { get; }
//     protected IServiceScope Scope { get; }
//     protected AppDbContext DbContext { get; }
//     protected Uri ServerAddress { get; }
//
//     protected IntegrationTestBase(bool useRealServer = false)
//     {
//         var databaseName = Guid.NewGuid().ToString();
//
//         if (!useRealServer)
//         {
//             Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
//             {
//                 builder.UseEnvironment("Testing");
//
//                 builder.ConfigureServices(services =>
//                 {
//                     var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
//                     if (descriptor != null) services.Remove(descriptor);
//
//                     services.AddDbContext<AppDbContext>(options =>
//                     {
//                         options.UseInMemoryDatabase(databaseName: databaseName);
//                     });
//                 });
//             });
//             Client = Factory.CreateClient();
//             Scope = Factory.Services.CreateScope();
//             DbContext = Scope.ServiceProvider.GetRequiredService<AppDbContext>();
//             return;
//         }
//
//         var webApplication = Program.CreateWebApplication([], (_, options) =>
//         {
//             options.UseInMemoryDatabase(databaseName: databaseName);
//         });
//       
//         Scope = webApplication.Services.CreateScope();
//         DbContext = Scope.ServiceProvider.GetRequiredService<AppDbContext>();
//     }
//
//     void IDisposable.Dispose()
//     {
//         Scope.Dispose();
//         Factory.Dispose();
//     }
// }
