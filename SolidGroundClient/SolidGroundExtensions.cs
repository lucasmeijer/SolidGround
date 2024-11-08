using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SolidGround;

namespace SolidGroundClient;

public static class SolidGroundExtensions
{
    public static void AddSolidGround<T>(this IServiceCollection serviceCollection) where T : SolidGroundVariables
    {
        serviceCollection.AddHttpClient<SolidGroundHttpClient>((sp,options) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();       
            options.BaseAddress = new Uri(config[SolidGroundConstants.SolidGroundBaseUrl] ?? throw new Exception("Missing SolidGroundBaseUrl"));
            options.DefaultRequestHeaders.Add("X-Api-Key", config[SolidGroundConstants.SolidGroundApiKey]  ?? throw new Exception("Missing SolidGroundApiKey"));
        });
        serviceCollection.AddHttpContextAccessor();
        serviceCollection.AddScoped<SolidGroundVariables, T>();
        serviceCollection.AddScoped<T>(sp =>
        {
            return sp.GetRequiredService<SolidGroundVariables>() as T ?? throw new InvalidOperationException();
        });
        
        serviceCollection.AddScoped<SolidGroundSession>(sp => 
        {
            var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
            var httpContext = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is null");
            return new(httpContext, sp.GetRequiredService<IConfiguration>(), 
                sp.GetRequiredService<SolidGroundVariables>(),
                sp.GetRequiredService<SolidGroundBackgroundService>()
            );
        });

        serviceCollection.AddSingleton<SolidGroundBackgroundService>();
        serviceCollection.AddHostedService<SolidGroundBackgroundService>(sp => sp.GetRequiredService<SolidGroundBackgroundService>());
    }

    public static void MapSolidGroundEndpoint(this WebApplication app)
    {
        //we need to use a custom middleware to enable buffering on the request, before the model binder starts reading from it
        //otherwise we can no longer recover that data. we should probably make this opt-in down the line to not pay this price
        //for every endpoint.
        app.Use(async (context, next) =>
        {
            context.Request.EnableBuffering();
            await next();
        });
        
        app.MapGet(EndPointRoute, (SolidGroundVariables variables) => new AvailableVariablesDto
        {
            StringVariables =
            [
                ..variables.Variables.Select(v => new StringVariableDto
                {
                    Name = v.Name,
                    Value = v.ValueAsString
                })
            ]
        });
    }

    public static string EndPointRoute => "solidground";
}