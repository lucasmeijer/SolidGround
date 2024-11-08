using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SolidGround;

namespace SolidGroundClient;

public class SolidGroundSession(
    HttpContext httpContext,
    IConfiguration config,
    SolidGroundVariables variables,
    SolidGroundBackgroundService solidGroundBackgroundService)
{
    HttpRequest Request => httpContext.Request;

    string? _serviceBaseUrl = config[SolidGroundConstants.SolidGroundBaseUrl]?.TrimEnd('/');
    
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
            Method = Request.Method,
            Route = Request.Path.Value ?? throw new ArgumentException("no request path"),
            QueryString = Request.QueryString.Value
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