using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using TurboFrames;

namespace SolidGround;

static class InputEndPoints
{
    public static class Routes
    {
        public static readonly RouteTemplate api_input = RouteTemplate.Create("/api/input");
        public static readonly RouteTemplate api_input_id = RouteTemplate.Create("/api/input/{id:int}");
        public static readonly RouteTemplate api_input_id_name = RouteTemplate.Create("/api/input/{id:int}/name");
        public static readonly RouteTemplate api_input_id_name_edit = RouteTemplate.Create("/api/input/{id:int}/name");
        public static readonly RouteTemplate api_input_id_tags = RouteTemplate.Create("/api/input/{id:int}/tags");
        public static readonly RouteTemplate api_input_id_details = RouteTemplate.Create("/api/input/{id:int}/details");
    }

    public static void MapInputEndPoints(this IEndpointRouteBuilder app)
    {
        app.MapPost(Routes.api_input, async (AppDbContext db, [FromBody] InputDto inputDto) =>
        {
            var input = await InputFor(inputDto);

            var outputComponents = inputDto.Output.OutputComponents.Select(c => new OutputComponent()
            {
                Name = c.Name,
                Value = c.Value
            });
            
            var variablesElement = inputDto.Output.StringVariables.Select(c => new StringVariable()
            {
                Name = c.Name,
                Value = c.Value
            });
            
            var output = new Output
            {
                Input = input,
                Components = [..outputComponents],
                StringVariables = [..variablesElement],
                Status = ExecutionStatus.Completed
            };
            db.Add(output);

            db.Add(new Execution
            {
                Outputs = [output],
                StartTime = DateTime.Now
            });

            await db.SaveChangesAsync();

            return TypedResults.Created(Routes.api_input_id.For(input.Id));
            
        }).DisableAntiforgery();
        
        app.MapGet(Routes.api_input_id, (int id) => new InputTurboFrame(id));
        app.MapGet(Routes.api_input_id_details, (int id) => new InputDetailsTurboFrame(id));
        app.MapGet(Routes.api_input_id_name, (int id) => new InputNameTurboFrame(id));
        app.MapGet(Routes.api_input_id_name_edit, (int id) => new InputNameEditTurboFrame(id));
        
        app.MapDelete(Routes.api_input_id, async (AppDbContext db, int id) =>
        {
            var input = await db.Inputs.FindAsync(id);
            if (input == null)
                return Results.NotFound($"Input {id} not found");
            db.Inputs.Remove(input);
            await db.SaveChangesAsync();

            return new TurboStream("remove", InputTurboFrame.TurboFrameIdFor(id));
        });
        
        app.MapPost(Routes.api_input_id_name, async (AppDbContext db, int id, NameUpdateDto nameUpdateDto) =>
        {
            var input = await db.Inputs.FindAsync(id);
            if (input == null)
                return Results.NotFound($"Input {id} not found");
            
            input.Name = nameUpdateDto.Name;
            await db.SaveChangesAsync();

            return new InputNameTurboFrame(id);
        }).DisableAntiforgery();

        app.MapPost(Routes.api_input_id_tags, async (AppDbContext db, int id, [FromForm] string tagData) =>
        {
            var input = await db.Inputs
                .Include(i => i.Tags)
                .FirstOrDefaultAsync(i => i.Id == id);
            if (input == null)
                return Results.BadRequest($"Input {id} not found");

            var json = JsonDocument.Parse(tagData).RootElement;

            if (json.TryGetProperty("add_tag", out var tagToAddElement))
            {
                input.Tags.Add(await TagHelper.FindTag(tagToAddElement, db));
                await db.SaveChangesAsync();
            }

            if (json.TryGetProperty("remove_tag", out var tagToRemoveElement))
            {
                var find = await TagHelper.FindTag(tagToRemoveElement, db);
                input.Tags.Remove(find);
                await db.SaveChangesAsync();
            }

            return new InputTagsTurboFrame(id);
        }).DisableAntiforgery();
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

    public record InputDto
    {
        [JsonPropertyName("request")]
        public required RequestDto Request { get; init; }
        
        [JsonPropertyName("output")]
        public required OutputDto Output { get; init; }
    }

    public record NameUpdateDto
    {
        [JsonPropertyName("name")]
        public required string Name { get; init; }
    }

    public record OutputDto
    {
        [JsonPropertyName("string_variables")]
        public required StringVariableDto[] StringVariables { get; init; }
        
        [JsonPropertyName("output_components")]
        public required OutputComponentDto[] OutputComponents { get; init; }
    }

    public record StringVariableDto
    {
        [JsonPropertyName("name")]
        public required string Name { get; init; }
        
        [JsonPropertyName("value")]
        public required string Value { get; init; }
    }

    public record OutputComponentDto
    {
        [JsonPropertyName("name")]
        public required string Name { get; init; }
        
        [JsonPropertyName("value")]
        public required string Value { get; init; }
    }

    public record RequestDto
    {
        [JsonPropertyName("body_base64")]
        public required string BodyBase64 { get; init; }
        
        [JsonPropertyName("content_type")]
        public required string ContentType { get; init; }
        
        [JsonPropertyName("route")]
        public required string Route { get; init; }
        
        [JsonPropertyName("base_path")]
        public required string BasePath { get; init; }
    }


    static async Task<Input> InputFor(InputDto inputDto)
    {
        var bodyBase64 = inputDto.Request.BodyBase64;
        var originalRequestContentType = inputDto.Request.ContentType;

        List<InputFile>? inputFiles = [];
        List<InputString> ? inputStrings =[];
        try
        {
            (inputFiles, inputStrings) = await ParseFormIntoStringsAndFiles(originalRequestContentType, bodyBase64);
        }
        catch (ArgumentException)
        {
            //apparently we're not a form
        }

        return new()
        {
            Files = inputFiles,
            Name = null,
            Strings = inputStrings,
            OriginalRequest_ContentType = originalRequestContentType,
            OriginalRequest_Body = bodyBase64,
            OriginalRequest_Route = inputDto.Request.Route,
            OriginalRequest_Host = inputDto.Request.BasePath
        };
    }
    
    static async Task<(List<InputFile> inputFiles, List<InputString> inputStrings)> ParseFormIntoStringsAndFiles(string s,
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


//
//
// [ApiController]
// [Route("/api/input")]
// public class InputController(AppDbContext db) : ControllerBase
// {
//     [HttpDelete("{id:int}")]
//     public async Task<IActionResult> Delete(int id)
//     {
//         var input = await db.Inputs.FindAsync(id);
//         if (input == null)
//             return NotFound($"Input {id} not found");
//         db.Inputs.Remove(input);
//         await db.SaveChangesAsync();
//
//         return new TurboStream("remove", InputTurboFrame.TurboFrameIdFor(id));
//     }
//
//     public static string ModifyInputRouteFor(int inputId) => $"/api/input/{inputId}";
//     
//     [HttpPost("{id:int}")]
//     public async Task<IActionResult> PatchInput(int id)
//     {
//         var input = await db.Inputs.FindAsync(id);
//         if (input == null)
//             return NotFound($"Input {id} not found");
//         
//         var form = await Request.ReadFormAsync();
//         if (!form.TryGetValue("name", out var name))
//             return BadRequest();
//
//         input.Name = name.ToString();
//         await db.SaveChangesAsync();
//         
//         return new InputNameTurboFrame(id);
//     }
//     
//     [IgnoreAntiforgeryToken]
//     public async Task<IActionResult> PostTags(int inputId, AppDbContext db, [FromForm] string tagData)
//     {
//         var input = await db.Inputs
//             .Include(i=>i.Tags)
//             .FirstOrDefaultAsync(i=>i.Id == inputId);
//         if (input == null)
//             return BadRequest($"Input {inputId} not found");
//
//         var json = JsonDocument.Parse(tagData).RootElement;
//     
//         if (json.TryGetProperty("add_tag", out var tagToAddElement))
//         {
//             input.Tags.Add(await TagHelper.FindTag(tagToAddElement, db));
//             await db.SaveChangesAsync();
//         }
//
//         if (json.TryGetProperty("remove_tag", out var tagToRemoveElement))
//         {
//             var find = await TagHelper.FindTag(tagToRemoveElement, db);
//             input.Tags.Remove(find);
//             await db.SaveChangesAsync();
//         }
//     
//         return new InputTagsTurboFrame(inputId);
//     }
//     
//     [HttpPost]
//     public async Task<IResult> CreateInput()
//     {
//         var jsonDoc = await JsonDocument.ParseAsync(Request.Body);
//         var root = jsonDoc.RootElement;
//
//         var outputElement = root.GetRequired<JsonElement>("outputs");
//         var variablesElement = root.GetRequired<JsonElement>("variables");
//         root.TryGetOptional("name", out string? name);
//
//         var input = await InputFor(root.GetRequired<JsonElement>("request"), name);
//
//         var output = new Output
//         {
//             Input = input,
//             Components = OutputComponentsFromJsonElement(outputElement),
//             StringVariables = VariablesFromJsonElement(variablesElement),
//             Status = ExecutionStatus.Completed
//         };
//         db.Add(output);
//
//         db.Add(new Execution
//         {
//             Outputs = [output],
//             StartTime = DateTime.Now
//         });
//
//         await db.SaveChangesAsync();
//         return Results.Ok();
//     }
//
//
//     public static List<OutputComponent> OutputComponentsFromJsonElement(JsonElement jsonElement)
//     {
//         return jsonElement
//             .EnumerateObject()
//             .Where(kvp => kvp.Value.ValueKind == JsonValueKind.String)
//             .Select(kvp =>
//             {
//                 var argValue = kvp.Value;
//                 var value = argValue.GetString();
//                 return new OutputComponent() { Name = kvp.Name, Value = value };
//             })
//             .ToList();
//     }
//
//     List<StringVariable> VariablesFromJsonElement(JsonElement jsonElement)
//     {
//         return jsonElement
//             .EnumerateObject()
//             .Where(kvp => kvp.Value.ValueKind == JsonValueKind.String)
//             .Select(kvp =>
//             {
//                 var argValue = kvp.Value;
//                 var value = argValue.GetString() ?? "null";
//                 return new StringVariable() { Name = kvp.Name, Value = value };
//             })
//             .ToList();
//     }
//
//     async Task<Input> InputFor(JsonElement requestElement, string? name)
//     {
//         var bodyBase64 = requestElement.GetRequired<string>("body_base64");
//         var originalRequestContentType = requestElement.GetRequired<string>("content_type");
//         var (inputFiles, inputStrings) = await ParseFormIntoStringsAndFiles(originalRequestContentType, bodyBase64);
//
//         return new()
//         {
//             Files = inputFiles,
//             Name = name,
//             Strings = inputStrings,
//             OriginalRequest_ContentType = originalRequestContentType,
//             OriginalRequest_Body = bodyBase64,
//             OriginalRequest_Route = requestElement.GetRequired<string>("route"),
//             OriginalRequest_Host = requestElement.GetRequired<string>("basepath"),
//         };
//     }
//
//     async Task<(List<InputFile> inputFiles, List<InputString> inputStrings)> ParseFormIntoStringsAndFiles(string s,
//         string bodyBase65)
//     {
//         var list = new List<InputFile>();
//         var inputStrings1 = new List<InputString>();
//
//         var boundary = HeaderUtilities.RemoveQuotes(MediaTypeHeaderValue.Parse(s).Boundary).Value;
//         if (boundary == null)
//             throw new ArgumentException("No boundary specified in content-type");
//
//         using var ms = new MemoryStream(Convert.FromBase64String(bodyBase65));
//         var reader = new MultipartReader(boundary, ms);
//
//         int fileCounter = 0;
//         int stringCounter = 0;
//         while (await reader.ReadNextSectionAsync() is { } section)
//         {
//             var contentDisposition = ContentDispositionHeaderValue.Parse(section.ContentDisposition);
//
//             var name = contentDisposition.Name.Value ?? throw new ArgumentException("Missing name in content-type");
//
//             if (contentDisposition.IsFileDisposition())
//             {
//                 list.Add(new()
//                 {
//                     Index = fileCounter++,
//                     Name = name,
//                     MimeType = contentDisposition.DispositionType.Value ??
//                                throw new ArgumentException($"element {name} has no disposition-type"),
//                     Bytes = await section.Body.ToBytesAsync()
//                 });
//                 continue;
//             }
//
//             if (contentDisposition.IsFormDisposition())
//             {
//                 using var streamReader = new StreamReader(section.Body);
//                 inputStrings1.Add(new()
//                 {
//                     Index = stringCounter++,
//                     Name = name,
//                     Value = await streamReader.ReadToEndAsync()
//                 });
//             }
//         }
//
//         return (list, inputStrings1);
//     }
// }