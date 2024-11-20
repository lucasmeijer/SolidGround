using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SolidGround;

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
        serviceCollection.AddAndInjectHostedService<SolidGroundBackgroundService>();
    }

    public static void AddAndInjectHostedService<TService>(this IServiceCollection serviceCollection) where TService : BackgroundService
    {
        serviceCollection.AddSingleton<TService>();
        serviceCollection.AddHostedService<TService>(sp => sp.GetRequiredService<TService>());
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
        
        app.UseResultException();
        
        app.MapGet(EndPointRoute, (IServiceProvider sp) =>
        {
            List<SolidGroundRouteInfo> result = [];
            foreach (var routeEndpoint in app.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>())
            {
                var metadata = routeEndpoint.Metadata.GetMetadata<SolidGroundMetadata>();
                if (metadata == null)
                    continue;
                var variables = metadata.For(sp);
                
                result.Add(new()
                {
                    Route = routeEndpoint.RoutePattern.RawText!,
                    StringVariables = variables.Properties.Select(p => new StringVariableDto()
                    {
                        Name = p.Name,
                        Value = variables.GetPropertyAsString(p),
                        Options = variables.GetPropertyOptions(p)
                    }).ToArray(),
                    ApplicationInformation = variables.ApplicationInformation,
                    PromptingGuidelines = variables.PromptingGuidelines,
                    EvaluationCriteria = variables.EvaluationCriteria,
                });
                
            }

            return result;
        });
    }
    
    public static string EndPointRoute => "solidground";
}