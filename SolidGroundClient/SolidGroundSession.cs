using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
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
    IServiceProvider serviceProvider)
{
    SolidGroundBackgroundService SolidGroundBackgroundService { get; } = serviceProvider.GetRequiredService<SolidGroundBackgroundService>(); 
    RequestDto? _reproducingRequest;

    List<OutputComponentDto> _outputComponents = [];

    SolidGroundVariables? _variables;

    List<string> _tagNames = [];
    string? _name = null;
    decimal? _costInDollar = null;
    string? _clientAppIdentifier = null;

    public void AddTag(string tagName) => _tagNames.Add(tagName);
    public void SetName(string name) => _name = name;
    public void SetClientAppIdentifier(string clientAppIdentifier) => _clientAppIdentifier = clientAppIdentifier;
    public void SetReproducingRequest(RequestDto request) => _reproducingRequest = request;

    public Task EnqueueSendFeedbackOnPreviousOutput(string clientAppOutputIdentifier, string feedback, string apiKey) =>
        SolidGroundBackgroundService.Enqueue(new()
        {
            ApiKey = apiKey,
            Method = HttpMethod.Post,
            Url = "/api/feedback",
            Payload = new SetFeedbackDto()
            {
                ClientAppIdentifier = clientAppOutputIdentifier,
                Feedback = feedback
            }
        });

    public async Task CompleteAsync(bool allowStorage, string apiKey)
    {
        if (IsSolidGroundInitiated)
            throw new ResultException(Results.Json(OutputDto()));
        
        if (allowStorage)
        {
            var reproducingRequest = _reproducingRequest ?? await httpContext.Request.Capture();
            await SolidGroundBackgroundService.Enqueue(SendRequestFor(reproducingRequest, apiKey));
        }
    }

    public bool IsSolidGroundInitiated => httpContext.Request.Headers.TryGetValue(SolidGroundConstants.SolidGroundInitiated, out _);

    SendRequest SendRequestFor(RequestDto capturedRequest, string apiKey) => new()
    {
        Method = HttpMethod.Post,
        Url = $"/api/input",
        Payload = new InputDto()
        {
            Name = _name,
            Request = capturedRequest,
            TagNames = [.._tagNames],
            Output = OutputDto(),
        },
        ApiKey = apiKey
    };

    OutputDto OutputDto() => new()
    {
        Cost = _costInDollar,
        OutputComponents = [.._outputComponents],
        StringVariables = VariablesAsStringVariableDtos(),
        ClientAppIdentifier = _clientAppIdentifier
    };

    StringVariableDto[] VariablesAsStringVariableDtos()
    {
        return _variables == null 
            ? throw new InvalidOperationException("Variables were not yet set nor get")
            : _variables.Properties
                .Select(p => new StringVariableDto()
                {
                    Name = p.Name,
                    Value = _variables.GetPropertyAsString(p),
                    Options = _variables.GetPropertyOptions(p)
                })
                .ToArray();
    }

    public void SetVariables(SolidGroundVariables variables)
    {
        if (_variables != null)
            throw new InvalidOperationException("variables already set");
        _variables = variables;
    }
    
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

    public void SetCost(decimal dollars) => _costInDollar = dollars;
    public void AddResult(string value, string? contentType=null) => AddArtifact("result", value, contentType);
    public void AddResultJson(object value) => AddArtifactJson("result", value);
    public void AddArtifact(string name, string value, string? contentType=null) => _outputComponents.Add(new() { Name = name, Value = value, ContentType = contentType ?? "text/plain"});
    public void AddArtifactJson(string name, object value) => AddArtifact(name,JsonSerializer.Serialize(value), "application/json");
}