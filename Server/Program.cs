using System.Runtime.CompilerServices;
using System.Text.Json;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SolidGround;
using SolidGround.Pages;
using SolidGroundClient;

[assembly: InternalsVisibleTo("Tests")]

CreateWebApplication<RealDbConfigurationForTenant>(args).Run();

[UsedImplicitly]
class RealDbConfigurationForTenant(IConfiguration configuration) : IDatabaseConfigurationForTenant
{
    public void Configure(DbContextOptionsBuilder options, Tenant? tenant)
    {
        if (tenant == null)
            throw new ArgumentNullException();
        options.ConfigureWarnings(b => b.Ignore(RelationalEventId.PendingModelChangesWarning));
        var persistentStorage = configuration["PERSISTENT_STORAGE"] ?? ".";
        options.UseSqlite($"Data Source={persistentStorage}/solid_ground_{tenant.Identifier}.db");
    }
}

interface IDatabaseConfigurationForTenant
{
    public void Configure(DbContextOptionsBuilder options, Tenant? tenant);
}

partial class Program
{
    public static WebApplication CreateWebApplication<T>(string[] args, Tenant? hardCodedTenant = null) where T : class, IDatabaseConfigurationForTenant
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSingleton<IDatabaseConfigurationForTenant, T>();
        builder.Services.AddDbContext<AppDbContext>((sp,db) =>
        {
            sp.GetRequiredService<IDatabaseConfigurationForTenant>().Configure(db, sp.GetService<Tenant>());
        });
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

        builder.Services.AddAndInjectHostedService<BackgroundWorkService>();
        builder.Services.AddHttpClient();
        builder.Services.AddHealthChecks().AddCheck("Health", () => HealthCheckResult.Healthy("OK"));
        builder.Services.AddControllersWithViews();
        builder.Services.AddControllers();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddMemoryCache();

        builder.Services.AddScoped<Tenant>(serviceProvider =>
        {
            if (hardCodedTenant != null)
                return hardCodedTenant;
            
            HttpRequest request = serviceProvider
                .GetRequiredService<IHttpContextAccessor>()
                .HttpContext?
                .Request ?? throw new Exception("no http context");
            
            if (request.Headers.TryGetValue("X-Api-Key", out var apiKey))
            {
                return Tenant.All.SingleOrDefault(t => t.ApiKey == apiKey.ToString()) ?? throw new BadHttpRequestException("API key is invalid");
            }

            foreach (var tenant in Tenant.All.OfType<SchrijfEvenMeeTenant>())
            {
                if (request.Host.Host == $"solidground.{tenant.Identifier}.schrijfevenmee.nl")
                    return tenant;
                if (request.Host.Host == $"{tenant.Identifier}.solidground.schrijfevenmee.nl")
                    return tenant;
            }
            
            return request.Host.Host switch
            {
                "solidground.flashcards.lucasmeijer.com" => new FlashCardsTenant(),
                "solidground.schrijfevenmee.nl" => new SchrijfEvenVanillaTenant(),
                "localhost" => new SchrijfEvenMeeTranscripts(),
                _ => throw new NotSupportedException("unknown domain: "+request.Host.Host)
            };
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

        app.UseResultException();
        
        app.MapGet("/", (AppState appState) => new SolidGroundPage("SolidGround", new IndexPageBodyContent(appState)));
        
        app.MapGet("/{input_id}", (AppState appState, string input_id) => new SolidGroundPage("SolidGround", new IndexPageBodyContent(appState)));

        app.MapTagsEndPoints();
        app.MapSearchEndPoints();
        app.MapInputEndPoints();
        app.MapOutputEndPoints();
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
