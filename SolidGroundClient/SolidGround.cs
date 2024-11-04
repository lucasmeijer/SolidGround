using System.Globalization;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
    HttpClient httpClient,
    HttpContext httpContext,
    IConfiguration config,
    SolidGroundVariables variables)
{
    HttpRequest Request => httpContext.Request;

    string? _serviceBaseUrl = config["SOLIDGROUND_BASE_URL"];
    
    string? _outputId = httpContext.Request.Headers.TryGetValue("SolidGroundOutputId", out var outputIdValues) ? outputIdValues.ToString() : null;
    JsonObject? _capturedRequest;
    JsonObject _outputs = new();
    JsonObject _variables = new();
    string? _name = null;
    
    public async Task CaptureRequestAsync()
    {
        Request.EnableBuffering();
        using var memoryStream = new MemoryStream();
        var pos = Request.Body.Position;
        Request.Body.Position = 0;
        await Request.Body.CopyToAsync(memoryStream);
        Request.Body.Position = pos;

        _capturedRequest = new()
        {
            ["body_base64"] = Convert.ToBase64String(memoryStream.ToArray()),
            ["content_type"] = Request.ContentType,
            ["basepath"] = Request.Scheme + "://" + Request.Host,
            ["route"] = Request.Path.Value,
        };

        foreach (var variable in variables.Variables)
        {
            if (!httpContext.Request.Headers.TryGetValue($"SolidGroundVariable_{variable.Name}", out var value))
                continue;
            variable.SetValue(Encoding.UTF8.GetString(Convert.FromBase64String(value.Single()?.Trim() ?? throw new InvalidOperationException())));
            _variables[variable.Name] = variable.ValueAsString;
        }
        
        if (_serviceBaseUrl != null)
            httpContext.Response.OnCompleted(SendPayload);
    }

    async Task SendPayload()
    {
        if (_outputId != null)
        {
            var jsonObject = new JsonObject()
            {
                ["outputs"] = _outputs,
                ["variables"] = _variables,
            };

            //This is a rerun being executed by solidground. In this scenario we only have to upload the output under the requested id.
            var serialize = JsonSerializer.Serialize(jsonObject);

            await httpClient.PostAsync($"{_serviceBaseUrl}/api/output/{_outputId}", new StringContent(serialize, Encoding.UTF8, "application/json"));
            return;
        }

        //this is the normal production flow where we emit a complete execution + input + output
        await httpClient.PostAsJsonAsync($"{_serviceBaseUrl}/api/input", new
        {
            request = _capturedRequest ?? throw new ArgumentException("Captured request is null"),
            outputs = _outputs,
            variables = _variables,
            name = _name
        });
    }

    public void AddName(string name) => _name = name;
    public void AddResult(string value) => AddArtifact("result", value);
    public void AddResultJson(object value) => AddArtifactJson("result", value);
    public void AddArtifact(string name, string value) => _outputs[name] = value;
    public void AddArtifactJson(string name, object value) => AddArtifact(name,JsonSerializer.Serialize(value));
}

public static class SolidGroundExtensions
{
    public static void AddSolidGround<T>(this IServiceCollection serviceCollection) where T : SolidGroundVariables
    {
        serviceCollection.AddHttpContextAccessor();
        serviceCollection.AddScoped<SolidGroundVariables, T>();
        serviceCollection.AddScoped<T>(sp => sp.GetRequiredService<SolidGroundVariables>() as T ?? throw new InvalidOperationException());
        
        serviceCollection.AddScoped<SolidGroundSession>(sp => 
        {
            var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
            var httpContext = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is null");
            return new(sp.GetRequiredService<HttpClient>(), httpContext, sp.GetRequiredService<IConfiguration>(), sp.GetRequiredService<SolidGroundVariables>());
        });
    }

    public static void MapSolidGroundEndpoint(this WebApplication app)
    {
        app.MapGet("/solidground", (SolidGroundVariables variables) => 
            variables.Variables
            .ToDictionary(
                v => v.Name,
                v => v.ValueAsString
            ));
    }
}