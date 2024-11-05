using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SolidGround;
using Xunit.Sdk;

public class ServerUnderTest_NetworkServer : IAsyncDisposable
{
    readonly WebApplication _webApplication;
    readonly IServiceScope _scope;
    public HttpClient Client { get; }
    public AppDbContext DbContext { get; }

    public ServerUnderTest_NetworkServer()
    {
        string databaseName = Guid.NewGuid().ToString();
        
        _webApplication = Program.CreateWebApplication([], (config, dboptions) =>
        {
            dboptions.UseInMemoryDatabase(databaseName: databaseName);
        });
        {
            var server = _webApplication.Services.GetRequiredService<IServer>();
            var addressesFeature = server.Features.Get<IServerAddressesFeature>() ?? throw new Exception();
            addressesFeature.Addresses.Add("http://127.0.0.1:0");
        }
        _webApplication.Start();
        
        var server2 = _webApplication.Services.GetRequiredService<IServer>();
        var addressesFeature2 = server2.Features.Get<IServerAddressesFeature>() ?? throw new Exception();
        var address = addressesFeature2.Addresses.First();
        Client = new HttpClient() { BaseAddress = new Uri(address) };
        
        _scope = _webApplication.Services.CreateScope();
        DbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }
    
    public async ValueTask DisposeAsync()
    {
        await DbContext.DisposeAsync();
        Client.Dispose();
        _scope.Dispose();
        await _webApplication.DisposeAsync();
    }
}

public abstract class IntegrationTestBase : IAsyncDisposable
{
    protected IntegrationTestBase()
    {
        ServerUnderTest = new();
        Client = ServerUnderTest.Client;
        DbContext = ServerUnderTest.DbContext;
    }

    ServerUnderTest_NetworkServer ServerUnderTest { get; }

    protected HttpClient Client {get; }

    protected AppDbContext DbContext { get; }
    
    public ValueTask DisposeAsync() => ServerUnderTest.DisposeAsync();
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
