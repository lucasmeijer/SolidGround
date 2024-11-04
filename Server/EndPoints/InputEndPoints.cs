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
    [SuppressMessage("ReSharper", "InconsistentNaming")] // ReSharper disable IdentifierTypo
    public static class Routes
    {
        public static readonly RouteTemplate api_input = RouteTemplate.Create("/api/input");
        public static readonly RouteTemplate api_input_id = RouteTemplate.Create("/api/input/{id:int}");
        public static readonly RouteTemplate api_input_id_name = RouteTemplate.Create("/api/input/{id:int}/name");
        public static readonly RouteTemplate api_input_id_name_edit = RouteTemplate.Create("/api/input/{id:int}/name");
        public static readonly RouteTemplate api_input_id_tags = RouteTemplate.Create("/api/input/{id:int}/tags");
        public static readonly RouteTemplate api_input_id_tags_tagid = RouteTemplate.Create("/api/input/{id:int}/tags/{tagid:int}");
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

        app.MapPost(Routes.api_input_id_tags, async (AppDbContext db, int id, AddTagToInputDto dto, HttpContext httpContext) =>
        {
            var input = await db.Inputs
                .Include(i => i.Tags)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (input == null)
                return Results.NotFound($"Input {id} not found");

            var tag = await db.Tags.FindAsync(dto.TagId);
            if (tag == null)
                return Results.NotFound($"Tag {dto.TagId} not found");

            input.Tags.Add(tag);
            await db.SaveChangesAsync();
            
            httpContext.Response.Headers.Location = Routes.api_input_id_tags_tagid.For(id, dto.TagId);
            httpContext.Response.StatusCode = StatusCodes.Status201Created;
            return new InputTagsTurboFrame(id);
        }).DisableAntiforgery();

        app.MapDelete(Routes.api_input_id_tags_tagid, async (int id, int tagid, AppDbContext db, HttpContext httpContext) =>
        {
            var input = await db.Inputs
                .Include(i => i.Tags)
                .FirstOrDefaultAsync(i => i.Id == id);
            if (input == null)
                return Results.NotFound($"input {id} not found");
            int removed = input.Tags.RemoveAll(t => t.Id == tagid);
            if (removed == 0)
                return Results.NotFound($"tag {tagid} was not attached to input {id}");
            await db.SaveChangesAsync();
            return new InputTagsTurboFrame(id);
        });
    }

    public record AddTagToInputDto
    {
        [JsonPropertyName("tagid")]
        public required int TagId { get; init; }
    }
    
    public record NameUpdateDto
    {
        [JsonPropertyName("name")]
        public required string Name { get; init; }
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