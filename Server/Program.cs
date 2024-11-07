using System.Net;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SolidGround;
using SolidGround.Pages;
using TurboFrames;
[assembly: InternalsVisibleTo("Tests")]

CreateWebApplication(args, (configuration, dboptions) =>
{
    var persistentStorage = configuration["PERSISTENT_STORAGE"] ?? ".";
    dboptions.UseSqlite($"Data Source={persistentStorage}/solid_ground.db");
}).Run();

public record AppState(int[] Tags, int[] Executions, string Search)
{
    public static AppState Default => new([], [-1], "");
}

class AppStateAccessor(IMemoryCache cache, IHttpContextAccessor accessor)
{
    public void Set(AppState state)
    {
        if (CacheKey == null)
            return;
        cache.Set(CacheKey, state);
    }

    string? CacheKey
    {
        get
        {
            var context = accessor.HttpContext;
            if (context == null)
                return null;

            if (!context.Request.Headers.TryGetValue("X-Tab-Id", out var tabId))
                return null;
            return "appstate_"+tabId;
        }
    }
    
    public AppState Get()
    {
        if (CacheKey == null)
            return AppState.Default;
        return cache.Get<AppState>(CacheKey) ?? AppState.Default;
    }
}

public partial class Program
{
    public static WebApplication CreateWebApplication(string[] args, Action<IConfiguration, DbContextOptionsBuilder> setupDb)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        builder.Services.AddDbContext<AppDbContext>(options =>
        {
            setupDb(builder.Configuration, options);
        });
    
        builder.Services.AddHttpClient();
        builder.Services.AddHealthChecks().AddCheck("Health", () => HealthCheckResult.Healthy("OK"));
        builder.Services.AddControllersWithViews();
        builder.Services.AddControllers();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddMemoryCache();
        builder.Services.AddScoped<AppStateAccessor>();
        builder.Services.AddScoped<AppState>(sp => sp.GetRequiredService<AppStateAccessor>().Get());
        
        var app = builder.Build();
        app.MapControllers();

        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            var dbContext = services.GetRequiredService<AppDbContext>();
            if (dbContext.Database.IsRelational())
                dbContext.Database.Migrate();
        }

// Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

//app.UseHttpMethodOverride(new() { FormFieldName = "_method"});
        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAuthorization();
        app.UseHealthChecks("/up");
        app.MapTurboFramesInSameAssemblyAs(typeof(Program));



//app.MapStaticAssets();
        app.UseStaticFiles();

//    .WithStaticAssets();

        app.MapGet("/", () => new IndexPage());


        app.MapTagsEndPoints();
        app.MapSearchEndPoints();
        app.MapInputEndPoints();
        app.MapOutputEndPoints();
        app.MapExperimentEndPoints();
        app.MapImagesEndPoints();
        app.MapExecutionsEndPoints();
        return app;
    }
}