using System.Globalization;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SolidGround;

namespace SolidGroundClient;

public class SolidGroundVariable
{
    public string Name { get; }
    
    internal string ValueAsString
    {
        get
        {
            if (ActualValue is float f)
                return f.ToString(CultureInfo.InvariantCulture);
            
            return ActualValue.ToString() ?? throw new InvalidOperationException();
        }
    }

    protected object ActualValue;

    protected SolidGroundVariable(string Name, object DefaultValue)
    {
        ActualValue = DefaultValue;
        this.Name = Name;
    }
    
    internal void SetValue(string valueAsString)
    {
        object ValueFor()
        {
            var myGenericTypeArgument = MyGenericTypeArgument();
            
            if (myGenericTypeArgument == typeof(float))
                return float.Parse(valueAsString, CultureInfo.InvariantCulture);

            if (myGenericTypeArgument == typeof(string))
                return valueAsString;
            
            throw new NotSupportedException($"Variables of type {myGenericTypeArgument} are not supported.");
        }

        ActualValue = ValueFor();
    }

    Type MyGenericTypeArgument() => GetType().GetGenericArguments().Single();
}
public class SolidGroundVariable<T>(string name, T defaultValue) : SolidGroundVariable(name, defaultValue ?? throw new ArgumentNullException(nameof(defaultValue)))
{
    public T Value => (T)ActualValue; 
}

public abstract class SolidGroundVariables
{
    internal SolidGroundVariable[] Variables
    {
        get
        {
            var fieldInfos = GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            
            return fieldInfos
                .Where(f => f.FieldType.IsAssignableTo(typeof(SolidGroundVariable)))
                .Select(f => f.GetValue(this))
                .Cast<SolidGroundVariable>()
                .ToArray();
        }
    }
}


public class SolidGroundSession(
    HttpContext httpContext,
    IConfiguration config,
    SolidGroundVariables variables,
    SolidGroundBackgroundService solidGroundBackgroundService)
{
    HttpRequest Request => httpContext.Request;

    string? _serviceBaseUrl = config[SolidGroundConstants.SolidGroundBaseUrl]?.TrimEnd("/");
    
    string? _outputId = httpContext.Request.Headers.TryGetValue(SolidGroundConstants.SolidGroundOutputId, out var outputIdValues) ? outputIdValues.ToString() : null;
    RequestDto? _capturedRequest;
 
    List<OutputComponentDto> _outputComponents = [];
    StringVariableDto[] _stringVariableDtos = [];

    public async Task CaptureRequestAsync()
    {
        if (_serviceBaseUrl == null)
            return;

        await Task.CompletedTask;

        var pos = Request.Body.Position;
        Request.Body.Position = 0;
        var ms = new MemoryStream();
        await Request.Body.CopyToAsync(ms);
        Request.Body.Position = pos;

        _capturedRequest = new()
        {
            BodyBase64 = Convert.ToBase64String(ms.ToArray()),
            ContentType = Request.ContentType,
            BasePath = Request.Scheme + "://" + Request.Host,
            Route = Request.Path.Value ?? throw new ArgumentException("no request path"),
        };

        foreach (var v in variables.Variables)
        {
            if (httpContext.Request.Headers.TryGetValue($"{SolidGroundConstants.HeaderVariablePrefix}{v.Name}", out var overridenValue))
                v.SetValue(Encoding.UTF8.GetString(Convert.FromBase64String(overridenValue.Single()?.Trim() ?? throw new InvalidOperationException())));
        }

        _stringVariableDtos = variables.Variables
            .Select(v => new StringVariableDto()
            {
                Name = v.Name,
                Value = v.ValueAsString
            })
            .ToArray();

        httpContext.Response.OnCompleted(SendPayload);
    }

    async Task SendPayload()
    {
        if (_outputId != null)
        {
            var outputDto = new OutputDto()
            {
                OutputComponents = [.._outputComponents],
                StringVariables = _stringVariableDtos
            };
            
            await solidGroundBackgroundService.Enqueue(new SendRequest() { Method = HttpMethod.Patch, Url = $"{_serviceBaseUrl}/api/outputs/{_outputId}", Payload = outputDto});
            return;
        }

        //this is the normal production flow where we emit a complete execution + input + output
        await solidGroundBackgroundService.EnqueueHttpPost($"{_serviceBaseUrl}/api/input", new InputDto()
        {
            Request = _capturedRequest ?? throw new InvalidOperationException(),
            Output = new()
            {
                OutputComponents = [.._outputComponents],
                StringVariables = _stringVariableDtos
            }
        });
    }

    public void AddResult(string value) => AddArtifact("result", value);
    public void AddResultJson(object value) => AddArtifactJson("result", value);
    public void AddArtifact(string name, string value) => _outputComponents.Add(new() { Name = name, Value = value });
    public void AddArtifactJson(string name, object value) => AddArtifact(name,JsonSerializer.Serialize(value));
}

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