using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace TurboFrames;

public static class TurboFrameExtensions
{
    public static void MapTurboFramesInSameAssemblyAs(this WebApplication app, Type type)
    {
        var turboFrameTypes = type.Assembly.GetExportedTypes()
            .Where(t => typeof(TurboFrame).IsAssignableFrom(t) && !t.IsAbstract).ToArray();

        foreach (var turboFrameType in turboFrameTypes)
            foreach (var routeAttribute in turboFrameType.GetCustomAttributes<RouteAttribute>())
                SetupMap(app, routeAttribute, turboFrameType);
    }

    static void SetupMap(WebApplication app, RouteAttribute routeAttribute, Type turboFrameType)
    {
        var parameters = turboFrameType.GetConstructors().SelectMany(c => c.GetParameters())
            .DistinctBy(p => p.Name)
            .ToDictionary(p => p.Name ?? throw new NotSupportedException("Parameter without name"), p => p.ParameterType);
        
        app.MapGet(routeAttribute.Template, (HttpContext context) =>
        {
            var routeValues = context.Request.RouteValues
                .Select(route_kvp =>
                {
                    var targetType = parameters.FirstOrDefault(parameter_kvp => parameter_kvp.Key == route_kvp.Key)
                                         .Value
                                     ?? throw new ArgumentException(
                                         $"RouteValue named {route_kvp.Key} did not have a matching parameter in any constructor of {turboFrameType.Name}");
                    return Convert.ChangeType(route_kvp.Value, targetType) ?? throw new ArgumentException("null values not supported");
                }).ToArray();
            return ActivatorUtilities.CreateInstance(context.RequestServices, turboFrameType, routeValues);
        });
        
       
    }
}

public class NotFoundException : Exception; 