using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SolidGround;
using Xunit;

class SolidGroundApplicationUnderTest : WebApplicationUnderTest<AppDbContext>
{
    SolidGroundApplicationUnderTest(WebApplication webApplication) : base(webApplication)
    {
    }

    protected override async Task<HttpClient> CreateHttpClient(Uri baseAddress)
    {
        var httpMessageHandler = new HttpClientHandler
        {
            CookieContainer = new(),
            UseCookies = true,
            //AllowAutoRedirect = false,
        };
        
        using var serviceScope = _webApplication.Services.CreateScope();
        var tenant = serviceScope.ServiceProvider.GetRequiredService<Tenant>();
        var apiKey = tenant.ApiKey;
        
        var client = new HttpClient(httpMessageHandler)
        {
            BaseAddress = baseAddress,
            DefaultRequestHeaders = { {"X-Api-Key", apiKey } }
        };

        //hit the login endpoint so we get assigned a cookie, so all subsequent tests work
        var httpResponseMessage = await client.PostAsync("/login", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("username", "your-username"),
            new KeyValuePair<string, string>("password", "1234"),
        }));
        httpResponseMessage.EnsureSuccessStatusCode();
        // //A succesful login will return .Found for redirect, which is technically not "succesful".
        // Assert.Equal(HttpStatusCode.Found, httpResponseMessage.StatusCode);
        return client;
    }

    public new static async Task<SolidGroundApplicationUnderTest> StartAsync(WebApplication webApplication)
    {
        var baseAddress = await StartAndGetAddress(webApplication);
        var result = new SolidGroundApplicationUnderTest(webApplication);
        result.HttpClient = await result.CreateHttpClient(baseAddress);
        return result;
    }
}

public class WebApplicationUnderTest<TDbContext> : IAsyncDisposable where TDbContext : notnull
{
    protected readonly WebApplication _webApplication;
    readonly IServiceScope _scope;
    public IServiceProvider Services { get; }
    public HttpClient HttpClient { get; set; } = null!;
    public TDbContext DbContext { get; }

    protected WebApplicationUnderTest(WebApplication webApplication)
    {
        _webApplication = webApplication;
        Services = webApplication.Services;
        _scope = Services.CreateScope();
        DbContext = typeof(TDbContext).IsAssignableTo(typeof(DbContext)) ? _scope.ServiceProvider.GetRequiredService<TDbContext>() : default!;
    }

    protected virtual Task<HttpClient> CreateHttpClient(Uri baseAddress) => Task.FromResult<HttpClient>(new() { BaseAddress = baseAddress });

    public static async Task<WebApplicationUnderTest<TDbContext>> StartAsync(WebApplication webApplication)
    {
        var baseAddress = await StartAndGetAddress(webApplication);
        var result = new WebApplicationUnderTest<TDbContext>(webApplication);
        result.HttpClient = await result.CreateHttpClient(baseAddress);
        return result;
    }

    protected static async Task<Uri> StartAndGetAddress(WebApplication webApplication)
    {
        var serviceProvider = webApplication.Services;
        
        var addressesFeatureAddresses = serviceProvider
            .GetRequiredService<IServer>()
            .Features
            .GetRequiredFeature<IServerAddressesFeature>()
            .Addresses;
        
        addressesFeatureAddresses.Add("http://127.0.0.1:0");
        
        await webApplication.StartAsync();
        var baseAddress = new Uri(addressesFeatureAddresses.First());
        return baseAddress;
    }


    public async ValueTask DisposeAsync()
    {
        await _webApplication.StopAsync();
        _scope.Dispose();
    }
}