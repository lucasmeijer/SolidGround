using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SolidGround;

namespace SolidGroundClient;

class SolidGroundMetadata(SolidGroundVariables Variables)
{
    public SolidGroundVariables For(IServiceProvider sp)
    {
        return Variables;
    }
}

public static class SolidGroundExtensions
{
    public static IEndpointConventionBuilder ExposeToSolidGround<T>(this IEndpointConventionBuilder builder) where T : SolidGroundVariables, new()
    {
        builder.Add(endpointBuilder =>
        {
            endpointBuilder.Metadata.Add(new SolidGroundMetadata(new T()));
        });
        return builder;
    }
    
    public static void AddSolidGround(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddHttpClient<SolidGroundHttpClient>((sp,options) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();       
            options.BaseAddress = new Uri(config[SolidGroundConstants.SolidGroundBaseUrl] ?? throw new Exception("Missing SolidGroundBaseUrl"));
            options.DefaultRequestHeaders.Add("X-Api-Key", config[SolidGroundConstants.SolidGroundApiKey]  ?? throw new Exception("Missing SolidGroundApiKey"));
        });
        serviceCollection.AddHttpContextAccessor();
        serviceCollection.AddScoped<SolidGroundSession>(sp => 
        {
            var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
            var httpContext = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is null");
            return new(httpContext, sp.GetRequiredService<IConfiguration>(), 
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
        
        app.MapGet(EndPointRoute, (IServiceProvider sp) => new JsonArray(app.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Select(e=>(e, e.Metadata.GetMetadata<SolidGroundMetadata>()))
            .Where(pair => pair.Item2 != null)
            .Select(pair => new JsonObject()
            {
                ["route"] = pair.Item1.RoutePattern.RawText,
                ["variables"] = SerializeToNode(pair, sp)
            })
            .ToArray<JsonNode>()));
    }

    static JsonNode? SerializeToNode((RouteEndpoint e, SolidGroundMetadata?) pair, IServiceProvider sp)
    {
        var result = new JsonObject();
        var vars = pair.Item2!.For(sp);
        foreach (var prop in SolidGroundSession.PropertyInfosFor(vars.GetType()))
        {
            var value = prop.GetValue(vars) ?? throw new Exception();
            result[prop.Name] = (string) value;
        }
        return result;
    }

    public static string EndPointRoute => "solidground";
}