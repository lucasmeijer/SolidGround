using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public class WebApplicationUnderTest<TDbContext> : IAsyncDisposable where TDbContext : notnull
{
    readonly WebApplication _webApplication;
    readonly IServiceScope _scope;
    public IServiceProvider Services { get; }
    public HttpClient HttpClient { get; }
    public TDbContext DbContext { get; }

    WebApplicationUnderTest(WebApplication webApplication, IServiceProvider services, HttpClient httpClient)
    {
        _webApplication = webApplication;
        Services = services;
        HttpClient = httpClient;
        _scope = services.CreateScope();
        DbContext = typeof(TDbContext).IsAssignableTo(typeof(DbContext)) ? _scope.ServiceProvider.GetRequiredService<TDbContext>() : default!;
    }

    public static async Task<WebApplicationUnderTest<TDbContext>> StartAsync(WebApplication webApplication)
    {
        var serviceProvider = webApplication.Services;
        
        var addressesFeatureAddresses = serviceProvider
            .GetRequiredService<IServer>()
            .Features
            .GetRequiredFeature<IServerAddressesFeature>()
            .Addresses;
        
        addressesFeatureAddresses.Add("http://127.0.0.1:0");
        await webApplication.StartAsync();
        
        var client = new HttpClient() { BaseAddress = new Uri(addressesFeatureAddresses.First()) };
        
        return new(webApplication, webApplication.Services, client);
    }
    
    public async ValueTask DisposeAsync()
    {
        await _webApplication.StopAsync();
        _scope.Dispose();
    }
}