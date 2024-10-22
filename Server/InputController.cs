using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using TurboFrames;

namespace SolidGround;

[ApiController]
[Route("/api/input")]
public class InputController(AppDbContext db) : ControllerBase
{
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var input = await db.Inputs.FindAsync(id);
        if (input == null)
            return NotFound($"Input {id} not found");
        db.Inputs.Remove(input);
        await db.SaveChangesAsync();

        return new TurboStream("remove", InputTurboFrame2.TurboFrameIdFor(id));
    }

    public static string ModifyInputRouteFor(int inputId) => $"/api/input/{inputId}";
    
    [HttpPost("{id:int}")]
    public async Task<IActionResult> PatchInput(int id)
    {
        var input = await db.Inputs.FindAsync(id);
        if (input == null)
            return NotFound($"Input {id} not found");
        
        var form = await Request.ReadFormAsync();
        if (!form.TryGetValue("name", out var name))
            return BadRequest();

        input.Name = name.ToString();
        await db.SaveChangesAsync();
        
        return new InputNameTurboFrame2(id);
    }
    
    [HttpPost]
    public async Task<IResult> CreateInput()
    {
        var jsonDoc = await JsonDocument.ParseAsync(Request.Body);
        var root = jsonDoc.RootElement;

        var outputElement = root.GetRequired<JsonElement>("outputs");
        var variablesElement = root.GetRequired<JsonElement>("variables");
        root.TryGetOptional("name", out string? name);

        var input = await InputFor(root.GetRequired<JsonElement>("request"), name);

        var output = new Output
        {
            Input = input,
            Components = OutputComponentsFromJsonElement(outputElement),
            StringVariables = VariablesFromJsonElement(variablesElement),
            Status = ExecutionStatus.Completed
        };
        db.Add(output);

        db.Add(new Execution
        {
            Outputs = [output],
            StartTime = DateTime.Now
        });

        await db.SaveChangesAsync();
        return Results.Ok();
    }


    public static List<OutputComponent> OutputComponentsFromJsonElement(JsonElement jsonElement)
    {
        return jsonElement
            .EnumerateObject()
            .Where(kvp => kvp.Value.ValueKind == JsonValueKind.String)
            .Select(kvp =>
            {
                var argValue = kvp.Value;
                var value = argValue.GetString();
                return new OutputComponent() { Name = kvp.Name, Value = value };
            })
            .ToList();
    }

    List<StringVariable> VariablesFromJsonElement(JsonElement jsonElement)
    {
        return jsonElement
            .EnumerateObject()
            .Where(kvp => kvp.Value.ValueKind == JsonValueKind.String)
            .Select(kvp =>
            {
                var argValue = kvp.Value;
                var value = argValue.GetString() ?? "null";
                return new StringVariable() { Name = kvp.Name, Value = value };
            })
            .ToList();
    }

    async Task<Input> InputFor(JsonElement requestElement, string? name)
    {
        var bodyBase64 = requestElement.GetRequired<string>("body_base64");
        var originalRequestContentType = requestElement.GetRequired<string>("content_type");
        var (inputFiles, inputStrings) = await ParseFormIntoStringsAndFiles(originalRequestContentType, bodyBase64);

        return new()
        {
            Files = inputFiles,
            Name = name,
            Strings = inputStrings,
            OriginalRequest_ContentType = originalRequestContentType,
            OriginalRequest_Body = bodyBase64,
            OriginalRequest_Route = requestElement.GetRequired<string>("route"),
            OriginalRequest_Host = requestElement.GetRequired<string>("basepath"),
        };
    }

    async Task<(List<InputFile> inputFiles, List<InputString> inputStrings)> ParseFormIntoStringsAndFiles(string s,
        string bodyBase65)
    {
        var list = new List<InputFile>();
        var inputStrings1 = new List<InputString>();

        var boundary = HeaderUtilities.RemoveQuotes(MediaTypeHeaderValue.Parse(s).Boundary).Value;
        if (boundary == null)
            throw new ArgumentException("No boundary specified in content-type");

        using var ms = new MemoryStream(Convert.FromBase64String(bodyBase65));
        var reader = new MultipartReader(boundary, ms);

        int fileCounter = 0;
        int stringCounter = 0;
        while (await reader.ReadNextSectionAsync() is { } section)
        {
            var contentDisposition = ContentDispositionHeaderValue.Parse(section.ContentDisposition);

            var name = contentDisposition.Name.Value ?? throw new ArgumentException("Missing name in content-type");

            if (contentDisposition.IsFileDisposition())
            {
                list.Add(new()
                {
                    Index = fileCounter++,
                    Name = name,
                    MimeType = contentDisposition.DispositionType.Value ??
                               throw new ArgumentException($"element {name} has no disposition-type"),
                    Bytes = await section.Body.ToBytesAsync()
                });
                continue;
            }

            if (contentDisposition.IsFormDisposition())
            {
                using var streamReader = new StreamReader(section.Body);
                inputStrings1.Add(new()
                {
                    Index = stringCounter++,
                    Name = name,
                    Value = await streamReader.ReadToEndAsync()
                });
            }
        }

        return (list, inputStrings1);
    }
}