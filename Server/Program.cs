using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SolidGround;
using SolidGround.Pages;

[assembly: InternalsVisibleTo("Tests")]

CreateWebApplication(args, (services, dboptions) =>
{
    var tenant = services.GetRequiredService<Tenant>();  //tenant is injected Scoped, and is different based ont he domain of the incoming reuest.
    var persistentStorage = services.GetRequiredService<IConfiguration>()["PERSISTENT_STORAGE"] ?? ".";
    dboptions.UseSqlite($"Data Source={persistentStorage}/solid_ground_{tenant.Identifier}.db");
}).Run();

//dboptions.ConfigureWarnings(b => b.Ignore(RelationalEventId.NonTransactionalMigrationOperationWarning));
public partial class Program
{
    public static WebApplication CreateWebApplication(string[] args, Action<IServiceProvider, DbContextOptionsBuilder> setupDb)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddDbContext<AppDbContext>(setupDb);

        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = builder.Environment.IsProduction()
                    ? CookieSecurePolicy.Always
                    : CookieSecurePolicy.None;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.LoginPath = "/login";
            });
        
        builder.Services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });
        
        builder.Services.AddHttpClient();
        builder.Services.AddHealthChecks().AddCheck("Health", () => HealthCheckResult.Healthy("OK"));
        builder.Services.AddControllersWithViews();
        builder.Services.AddControllers();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddMemoryCache();

        builder.Services.AddScoped<Tenant>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            
            HttpRequest httpContextRequest = serviceProvider
                .GetRequiredService<IHttpContextAccessor>()
                .HttpContext?
                .Request ?? throw new Exception("no http context");

            logger.LogInformation($"Incoming 1 {httpContextRequest.Path}");
            
            if (httpContextRequest.Headers.TryGetValue("X-Api-Key", out var apiKey))
            {
                Tenant[] tenants = [new SchrijfEvenMeeHuisArtsTenant(), new FlashCardsTenant()];
                var tenant = tenants.SingleOrDefault(t => t.ApiKey == apiKey.ToString());

                if (tenant == null)
                {
                    logger.LogInformation("Incoming 2 Api-Key Invalid");
                    throw new BadHttpRequestException("API key is invalid");
                }
                logger.LogInformation($"Incoming 2 Api-Key from {tenant.Identifier}");

                return tenant;
            }

            Tenant tenant1 = httpContextRequest.Host.Host switch
            {
                "solidground.flashcards.lucasmeijer.com" => new FlashCardsTenant(),
                "solidground.huisarts.schrijfevenmee.nl" => new SchrijfEvenMeeHuisArtsTenant(),
                "localhost" => new SchrijfEvenMeeHuisArtsTenant(),
                _ => throw new NotSupportedException("unknown domain: "+httpContextRequest.Host.Host)
            };
            
            logger.LogInformation($"Incoming without API key from {tenant1.Identifier} on {httpContextRequest.Host.Host}");
            return tenant1;
        });
        
        builder.Services.AddScoped<AppState>(sp =>
        {
            var accessor = sp.GetRequiredService<IHttpContextAccessor>();
            var context = accessor.HttpContext;
            if (context == null)
                return AppState.Default;

            if (!context.Request.Headers.TryGetValue("X-App-State", out var appStateJson))
                return AppState.Default;

            return JsonSerializer.Deserialize<AppState>(appStateJson.ToString(), JsonSerializerOptions.Web) ?? AppState.Default;
        });
        
        var app = builder.Build();
        app.MapControllers();

        // {
        //     using var scope = app.Services.CreateScope();
        //     var services = scope.ServiceProvider;
        //     var dbContext = services.GetRequiredService<AppDbContext>();
        //     if (dbContext.Database.IsRelational())
        //             dbContext.Database.Migrate();
        // }

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseRouting();

        //static files has to be before authorization.
        app.UseStaticFiles();

        app.UseAuthorization();
        app.UseHealthChecks("/up");

        app.UseStaticFiles();

        app.MapGet("/", (AppState appState) => new SolidGroundPage("SolidGround", new IndexPageBodyContent(appState)));

        app.MapTagsEndPoints();
        app.MapSearchEndPoints();
        app.MapInputEndPoints();
        app.MapOutputEndPoints();
        app.MapExperimentEndPoints();
        app.MapImagesEndPoints();
        app.MapExecutionsEndPoints();
        app.MapLoginEndPoints();
        return app;
    }
}

public static class RouteHandlerBuilderExtensions
{
    public static RouteHandlerBuilder RequireTenantApiKey(this RouteHandlerBuilder builder) =>
        builder
            //first lets remove the default requirement for cookie authorization:
            .AllowAnonymous()
            //and then add our own api checks.
            .AddEndpointFilter(async (context, next) =>
        {
            var tenant = context.HttpContext.RequestServices.GetRequiredService<Tenant>();
            if (!context.HttpContext.Request.Headers.TryGetValue("X-Api-Key", out var providedApiKey))
                return Results.Unauthorized();

            if (!tenant.ApiKey.Equals(providedApiKey))
            {
                return Results.Unauthorized();
            }

            return await next(context);
        });
}
