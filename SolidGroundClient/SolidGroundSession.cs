using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
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
            //BasePath = request.Scheme + "://" + request.Host,
            Method = request.Method,
            Route = request.Path.Value ?? throw new ArgumentException("no request path"),
            QueryString = request.QueryString.Value
        };
    }
}

public record SolidGroundSessionAccessor(SolidGroundSession? Session);

public class SolidGroundSession(HttpContext httpContext,
    IServiceProvider serviceProvider,
    string apiKey)
{
    SolidGroundBackgroundService SolidGroundBackgroundService { get; } = serviceProvider.GetRequiredService<SolidGroundBackgroundService>(); 
    RequestDto? _reproducingRequest;

    string? _outputId = httpContext.Request.Headers.TryGetValue(SolidGroundConstants.SolidGroundOutputId, out var outputIdValues) ? outputIdValues.ToString() : null;

    List<OutputComponentDto> _outputComponents = [];

    SolidGroundVariables? _variables;

    List<string> _tagNames = [];
    string? _name = null;
    
    public void AddTag(string tagName) => _tagNames.Add(tagName);
    public void SetName(string name) => _name = name;
    
    public void SetReproducingRequest(RequestDto request) => _reproducingRequest = request;
    
    public async Task CompleteAsync(bool allowStorage)
    {
        if (allowStorage || IsSolidGroundInitiated)
        {
            var reproducingRequest = _reproducingRequest ?? await httpContext.Request.Capture();
            await SolidGroundBackgroundService.Enqueue(SendRequestFor(reproducingRequest));
        }
    }
    
    SendRequest SendRequestFor(RequestDto capturedRequest) => _outputId != null
        ? new SendRequest()
        {
            Method = HttpMethod.Patch,
            Url = $"/api/outputs/{_outputId}",
            Payload = OutputDto(),
            ApiKey = apiKey
        }

        : new SendRequest()
        {
            Method = HttpMethod.Post,
            Url = $"/api/input",
            Payload = new InputDto()
            {
                Name = _name,
                Request = capturedRequest,
                TagNames = [.._tagNames],
                Output = OutputDto()
            },
            ApiKey = apiKey
        };

    OutputDto OutputDto() => new()
    {
        OutputComponents = [.._outputComponents],
        StringVariables = _variables == null //this can happen in cases like SchrijfEvenMee feedback, where we only have feedback, but not the data of the original run. 
            ? [] 
            : _variables.Properties
            .Select(p => new StringVariableDto()
            {
                Name = p.Name,
                Value = _variables.GetPropertyAsString(p),
                Options = _variables.GetPropertyOptions(p)
            })
            .ToArray()
    };

    public T GetVariables<T>() where T : SolidGroundVariables, new()
    {
        if (_variables is T t)
            return t;
        if (_variables != null)
            throw new NotSupportedException($"Mismatched variables types {typeof(T).Name} and {_variables.GetType().Name}");
        
        var hd = httpContext.Request.Headers;
        var v = new T();
        _variables = v;
        foreach (var p in v.Properties)
        {
            if (hd.TryGetValue($"{SolidGroundConstants.HeaderVariablePrefix}{p.Name}", out var value)) 
                _variables.SetPropertyAsString(p, Encoding.UTF8.GetString(Convert.FromBase64String(value.ToString())));
        }
        
        return v;
    }

    bool IsSolidGroundInitiated => _outputId != null;

    public void AddResult(string value, string? contentType=null) => AddArtifact("result", value, contentType);
    public void AddResultJson(object value) => AddArtifactJson("result", value);
    public void AddArtifact(string name, string value, string? contentType=null) => _outputComponents.Add(new() { Name = name, Value = value, ContentType = contentType ?? "text/plain"});
    public void AddArtifactJson(string name, object value) => AddArtifact(name,JsonSerializer.Serialize(value), "application/json");
}