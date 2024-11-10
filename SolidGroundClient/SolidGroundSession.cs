using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SolidGround;

namespace SolidGroundClient;

static class RequestDtoExtensions
{
    public static async Task<RequestDto> Capture(this HttpRequest request)
    {
        var pos = request.Body.Position;
        request.Body.Position = 0;
        var ms = new MemoryStream();
        await request.Body.CopyToAsync(ms);
        request.Body.Position = pos;

        return new()
        {
            BodyBase64 = Convert.ToBase64String(ms.ToArray()),
            ContentType = request.ContentType,
            BasePath = request.Scheme + "://" + request.Host,
            Method = request.Method,
            Route = request.Path.Value ?? throw new ArgumentException("no request path"),
            QueryString = request.QueryString.Value
        };
    }
}

public class SolidGroundSession(HttpContext httpContext,
    IConfiguration config,
    SolidGroundBackgroundService solidGroundBackgroundService)
{
    string _serviceBaseUrl = test(config);

    static string test(IConfiguration config)
    {
        return config[SolidGroundConstants.SolidGroundBaseUrl]?.TrimEnd('/') ?? throw new ArgumentException($"{SolidGroundConstants.SolidGroundBaseUrl} not found");
    }

    string? _outputId = test2(httpContext);

    static string? test2(HttpContext httpContext)
    {
        return httpContext.Request.Headers.TryGetValue(SolidGroundConstants.SolidGroundOutputId, out var outputIdValues) ? outputIdValues.ToString() : null;
    }

    List<OutputComponentDto> _outputComponents = [];

    SolidGroundVariables? _variables;
   
    public async Task CompleteAsync(bool allowStorage)
    {
        if (allowStorage || IsSolidGroundInitiated)
        {
            //make sure we capture the request right here and now, because when OnCompleted runs the stream will be dead.
            var capturedRequest = await httpContext.Request.Capture();
            httpContext.Response.OnCompleted(() => solidGroundBackgroundService.Enqueue(SendRequestFor(capturedRequest)));
        }
    }
    
    SendRequest SendRequestFor(RequestDto capturedRequest) => _outputId != null
        ? new SendRequest()
        {
            Method = HttpMethod.Patch,
            Url = $"{_serviceBaseUrl}/api/outputs/{_outputId}",
            Payload = OutputDto()
        }

        : new SendRequest()
        {
            Method = HttpMethod.Post,
            Url = $"{_serviceBaseUrl}/api/input",
            Payload = new InputDto()
            {
                Request = capturedRequest,
                Output = OutputDto()
            }
        };

    OutputDto OutputDto() => new()
    {
        OutputComponents = [.._outputComponents],
        StringVariables = GetStringVariableDtos()
    };
    
    internal static IEnumerable<PropertyInfo> PropertyInfosFor(Type t) =>
        t
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(f => f.PropertyType.IsAssignableTo(typeof(string)));

    public T GetVariables<T>() where T : SolidGroundVariables, new()
    {
        if (_variables is T t)
            return t;
        if (_variables != null)
            throw new NotSupportedException($"Mismatched variables types {typeof(T).Name} and {_variables.GetType().Name}");
        
        var hd = httpContext.Request.Headers;
        var v = new T();
        _variables = v;
        foreach (var p in PropertyInfosFor(typeof(T)))
        {
            if (hd.TryGetValue($"{SolidGroundConstants.HeaderVariablePrefix}{p.Name}", out var value))
                p.SetValue(_variables, Encoding.UTF8.GetString(Convert.FromBase64String(value.ToString())));
        }
        
        return v;
    }

    StringVariableDto[] GetStringVariableDtos() =>
        PropertyInfosFor((_variables ?? throw new InvalidOperationException()).GetType())
            .Select(p => new StringVariableDto()
            {
                Name = p.Name,
                Value = (string)(p.GetValue(_variables) ?? throw new InvalidOperationException())
            })
            .ToArray();
    
    bool IsSolidGroundInitiated => _outputId != null;


    public void AddResult(string value) => AddArtifact("result", value);
    public void AddResultJson(object value) => AddArtifactJson("result", value);
    public void AddArtifact(string name, string value) => _outputComponents.Add(new() { Name = name, Value = value });
    public void AddArtifactJson(string name, object value) => AddArtifact(name,JsonSerializer.Serialize(value));
}