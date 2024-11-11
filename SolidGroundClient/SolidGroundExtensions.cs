using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SolidGroundClient;

class SolidGroundMetadata
{
    readonly Func<IServiceProvider,SolidGroundVariables> _variablesProvider;

    public SolidGroundMetadata(SolidGroundVariables variables) => _variablesProvider = _ => variables;

    public SolidGroundMetadata(Func<IServiceProvider,SolidGroundVariables> variablesProvider) => _variablesProvider = variablesProvider;

    public SolidGroundVariables For(IServiceProvider sp) => _variablesProvider.Invoke(sp);
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
    public static IEndpointConventionBuilder ExposeToSolidGround(this IEndpointConventionBuilder builder, Func<IServiceProvider,SolidGroundVariables> variablesProvider)
    {
        builder.Add(endpointBuilder =>
        {
            endpointBuilder.Metadata.Add(new SolidGroundMetadata(variablesProvider));
        });
        return builder;
    }

    public static void AddSolidGround(this IServiceCollection serviceCollection, Func<IServiceProvider, string?> apiKeyProvider)
    {
        serviceCollection.AddHttpClient<SolidGroundHttpClient>((sp,options) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();       
            options.BaseAddress = new Uri(config[SolidGroundConstants.SolidGroundBaseUrl] ?? "https://solidground.flashcards.lucasmeijer.com");
            //options.DefaultRequestHeaders.Add("X-Api-Key", config[SolidGroundConstants.SolidGroundApiKey]  ?? throw new Exception("Missing SolidGroundApiKey"));
        });
        
        serviceCollection.AddHttpContextAccessor();
        serviceCollection.AddScoped<SolidGroundSessionAccessor>(sp =>
        {
            var httpContext = sp.GetRequiredService<IHttpContextAccessor>().HttpContext ?? throw new InvalidOperationException();
            var apiKey = apiKeyProvider(sp);
            if (apiKey == null)
                return new(null);
            return new(new(httpContext, sp, apiKey));
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
        foreach (var prop in vars.Properties)
        {
            var jsonObject = new JsonObject() 
            {  
                ["type"] = prop.PropertyType == typeof(bool) ? "bool" : "string",
                ["value"] = vars.GetPropertyAsString(prop) ?? throw new Exception()
            };
            if (prop.PropertyType.IsEnum)
                jsonObject["options"] = new JsonArray([..Enum.GetNames(prop.PropertyType)]);
            
            result[prop.Name] = jsonObject;
        }
        return result;
    }

    public static string EndPointRoute => "solidground";
}