using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Net.Http.Headers;
using SolidGround;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

var persistentStorage = builder.Configuration["PERSISTENT_STORAGE"] ?? ".";
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite($"Data Source={persistentStorage}/solid_ground.db"));
builder.Services.AddHttpClient();
builder.Services.AddHealthChecks().AddCheck("Health", () => HealthCheckResult.Healthy("OK"));
builder.Services.AddControllersWithViews();
builder.Services.AddControllers();
var app = builder.Build();
app.MapControllers();
// Apply any pending migrations
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.UseHealthChecks("/up");


app.MapGet("/images/{inputId:int}/{imageIndex}", async (int inputId, int imageIndex, AppDbContext db, HttpContext httpContext) =>
{
    var inputFile = await db.InputFiles
        .Include(f => f.Input)
        .FirstOrDefaultAsync(file => file.InputId == inputId && file.Index == imageIndex);
    if (inputFile == null)
        return Results.NotFound();
    
    httpContext.Response.ContentType = inputFile.MimeType;
    await httpContext.Response.Body.WriteAsync(inputFile.Bytes);
    return Results.Empty;
});

app.MapPatch("/api/executions/{id}", async (int id,RestPatchExecutionRequest req, AppDbContext db) =>
{
    var execution = await db.Executions.FindAsync(id);
    if (execution == null)
        return Results.NotFound($"Execution with ID {id} not found.");
    
    execution.IsReference = req.IsReference;
    await db.SaveChangesAsync();
    return Results.Ok(execution);
});

app.MapDelete("/api/executions/{id}", async (int id, AppDbContext db) =>
{
    var execution = await db.Executions.FindAsync(id) ?? throw new BadHttpRequestException("Execution with ID " + id + " not found.");
    await db.Entry(execution).Collection(e => e.Outputs).LoadAsync();
    
    db.Executions.Remove(execution);
    await db.SaveChangesAsync();

    var sb = new StringBuilder();
    foreach(var o in execution.Outputs)
    {
        sb.AppendLine($"<turbo-stream action=\"remove\" target=\"output_{o.Id}\"></turbo-stream>");
    }
    return Results.Content(sb.ToString(), "text/vnd.turbo-stream.html");    
});


app.MapDelete("/api/output/{id}", async (int id, AppDbContext db) =>
{
    var obj = db.Outputs.Find(id);
    if (obj == null)
        return Results.BadRequest();
    
    db.Outputs.Remove(obj);
    await db.SaveChangesAsync();
    return Results.Content($"<turbo-stream action=\"remove\" target=\"output_{id}\"></turbo-stream>", "text/vnd.turbo-stream.html");
});


app.MapPost("/api/output/{id}", async (int id, HttpRequest req, AppDbContext db) =>
{
    var jsonDoc = await JsonDocument.ParseAsync(req.Body);
    
    var output = db.Outputs.Find(id);
    if (output == null)
        return Results.BadRequest($"Output {id} not found");

    if (!jsonDoc.RootElement.TryGetProperty("outputs", out var outputElement))
        return Results.BadRequest("output element not found");
    
    output.Components = OutputComponentsFromJsonElement(outputElement);
    output.Status = ExecutionStatus.Completed;
    await db.SaveChangesAsync();
    return Results.Ok();
});

app.MapPost("/api/search/tags", async (AppDbContext db, [FromForm] string tagData) =>
{
    var json = JsonDocument.Parse(tagData).RootElement;
    if (!json.TryGetProperty("new_tags", out var tagToAddElement))
        throw new BadHttpRequestException("no new_tags found");
    
    var tags = tagToAddElement.EnumerateArray().Select(t => FindTag(t, db));
    return new TurboStream([await InputTags.ForSearchTags([..tags], db)]);
}).DisableAntiforgery();

Tag FindTag(JsonElement tagidElement, AppDbContext appDbContext)
{
    if (tagidElement.ValueKind != JsonValueKind.Number)
        throw new BadHttpRequestException("Tag not a number");

    var tagid = tagidElement.GetInt32();
    var t = appDbContext.Tags.Find(tagid);
    if (t == null)
        throw new BadHttpRequestException("Tag not found");
    return t;
}

app.MapPost("/api/input/{id}/tags", async (int id, HttpRequest req, AppDbContext db, [FromForm] string tagData) =>
{
    var input = await db.Inputs.FindAsync(id);
    if (input == null)
        return Results.BadRequest($"Input {id} not found");
    await db.Entry(input).Collection(i => i.Tags).LoadAsync();

    var json = JsonDocument.Parse(tagData).RootElement;
    
    if (json.TryGetProperty("add_tag", out var tagToAddElement))
    {
        input.Tags.Add(FindTag(tagToAddElement, db));
        await db.SaveChangesAsync();
    }

    if (json.TryGetProperty("remove_tag", out var tagToRemoveElement))
    {
        var find = FindTag(tagToRemoveElement, db);
        input.Tags.Remove(find);
        await db.SaveChangesAsync();
    }
    return (IResult) input.TagsViewData(await db.Tags.ToArrayAsync());
}).DisableAntiforgery();

app.MapPost("/api/input", async (HttpRequest req, AppDbContext db) =>
{
    var jsonDoc = await JsonDocument.ParseAsync(req.Body);
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
});

app.MapGet("/runexperimentform/{output_id}", async (int output_id, AppDbContext db, HttpClient httpClient, IConfiguration config) =>
{
    var output = await db.Outputs.FindAsync(output_id) ?? throw new BadHttpRequestException("Output " + output_id + " not found.");
    await db.Entry(output).Collection(o=>o.StringVariables).LoadAsync();
    var outputStringVariables = output.StringVariables;
    
    return await RunExperimentHelper(httpClient, outputStringVariables, config);
});

app.MapGet("/runexperimentform", async (HttpClient httpClient, IConfiguration config) => await RunExperimentHelper(httpClient,[], config));

async Task<TurboFrame> RunExperimentHelper(HttpClient httpClient, List<StringVariable> overrideVariables, IConfiguration config)
{
    var requestUri = $"{config.GetMandatory("SOLIDGROUND_TARGET_APP")}/solidground";
    var result = await httpClient.GetAsync(requestUri);
    result.EnsureSuccessStatusCode();
    
    var jdoc = await JsonDocument.ParseAsync(await result.Content.ReadAsStreamAsync());

    var d = jdoc
        .RootElement
        .EnumerateObject()
        .ToDictionary(k => k.Name, v => v.Value.GetString() ?? throw new InvalidOperationException());

    foreach (var overrideVariable in overrideVariables)
        d[overrideVariable.Name] = overrideVariable.Value;
    
    return new RunExperimentForm(d.ToArray());
}

app.MapDelete("/api/input/{id}", async (int id, AppDbContext db) =>
{
    var input = await db.Inputs.FindAsync(id);
    if (input == null)
        return Results.BadRequest($"Input {id} not found");
    db.Inputs.Remove(input);
    await db.SaveChangesAsync();
    return Results.Content($"<turbo-stream action=\"remove\" target=\"input_{id}\"></turbo-stream>", "text/vnd.turbo-stream.html");
});

app.MapGet("/api/input/{id}", async (int id, AppDbContext db) =>
{
    var input = await db.CompleteInputs.FirstOrDefaultAsync(i => i.Id == id) ?? throw new BadHttpRequestException("input not found");
    
    return new InputTurboFrame(input, await db.Tags.ToArrayAsync());
});

// app.MapPost("/api/executions", async (RestExecution restExecution, AppDbContext db, HttpClient httpClient) =>
// {
//     var inputsToOutputs = restExecution.InputIds.ToDictionary(id => id, OutputFor);
//     var execution = new Execution()
//     {
//         StartTime = DateTime.Now,
//         Name = string.IsNullOrEmpty(restExecution.Name) ? null : restExecution.Name, 
//         Outputs = [
//             ..inputsToOutputs.Values
//         ]
//     };
//     
//     db.Executions.Add(execution);
//     await db.SaveChangesAsync();
//
//     foreach (var (inputId, output) in inputsToOutputs)
//     {
//         _ = Task.Run(() => ExecutionForInput(inputId, output, restExecution.Endpoint));
//     }
//
//     return;
//
//     Output OutputFor(int inputId) => new()
//     {
//         InputId = inputId,
//         Status = ExecutionStatus.Started,
//         Components = []
//     };
// });

//app.MapStaticAssets();
app.UseStaticFiles();
app.MapRazorPages();
//    .WithStaticAssets();


app.Lifetime.ApplicationStarted.Register(() =>
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Tags.FirstOrDefault(t => t.Name == "Hester") == null)
    {
        db.Tags.Add(new Tag { Name = "Hester" });
        db.SaveChanges();
    }
    if (db.Tags.FirstOrDefault(t => t.Name == "Lucas") == null)
    {
        db.Tags.Add(new Tag { Name = "Lucas" });
        db.SaveChanges();
    }
    if (db.Tags.FirstOrDefault(t => t.Name == "Gemeente") == null)
    {
        db.Tags.Add(new Tag { Name = "Gemeente" });
        db.SaveChanges();
    }
});

app.Run();
return;


List<OutputComponent> OutputComponentsFromJsonElement(JsonElement jsonElement)
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
            var value = argValue.GetString();
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

async Task<(List<InputFile> inputFiles, List<InputString> inputStrings)> ParseFormIntoStringsAndFiles(string s, string bodyBase65)
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
                MimeType = contentDisposition.DispositionType.Value ?? throw new ArgumentException($"element {name} has no disposition-type"),
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

record RestPatchExecutionRequest(bool IsReference);