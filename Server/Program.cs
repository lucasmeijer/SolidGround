using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using SolidGround;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

var persistentStorage = builder.Configuration["PERSISTENT_STORAGE"] ?? ".";
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite($"Data Source={persistentStorage}/solid_ground.db"));
builder.Services.AddHttpClient();
var app = builder.Build();

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

app.MapGet("/images/{inputId:int}/{imageIndex}", async (int inputId, int imageIndex, AppDbContext db, HttpContext httpContext) =>
{
    var inputFile = await db.InputFiles.FirstOrDefaultAsync(file => file.InputId == inputId && file.Index == imageIndex);
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

app.MapDelete("/api/output/{id}", async (int id, AppDbContext db) =>
{
    var obj = db.Outputs.Find(id);
    if (obj == null)
        return Results.BadRequest();

    db.Outputs.Remove(obj);
    await db.SaveChangesAsync();
    return Results.Ok();
});

app.MapPost("/api/output/{id}", async (int id, HttpRequest req, AppDbContext db) =>
{
    var jsonDoc = await JsonDocument.ParseAsync(req.Body);
    
    var output = db.Outputs.Find(id);
    if (output == null)
        return Results.BadRequest($"Output {id} not found");

    output.Components = OutputComponentsFromJsonElement(jsonDoc.RootElement);
    output.Status = ExecutionStatus.Completed;
    await db.SaveChangesAsync();
    return Results.Ok();
});

app.MapPost("/api/input", async (HttpRequest req, AppDbContext db) =>
{
    var jsonDoc = await JsonDocument.ParseAsync(req.Body);
    var root = jsonDoc.RootElement;
    
    File.WriteAllText("/Users/lucas/SolidGround/sample_ingress.json", JsonSerializer.Serialize(root));
    
    var outputElement = root.GetRequired<JsonElement>("output");
    
    var input = await InputFor(root.GetRequired<JsonElement>("request"));

    db.Add(new Execution
    {
        Outputs = [new Output
        {
            Input = input,
            Components = OutputComponentsFromJsonElement(outputElement), 
            Status = ExecutionStatus.Completed
        }],
        Name = root.GetRequired<string>("name"),
        StartTime = DateTime.Now
    });
    
    await db.SaveChangesAsync();
});

app.MapPost("/api/executions", async (RestExecution restExecution, AppDbContext db, HttpClient httpClient) =>
{
    var inputsToOutputs = restExecution.InputIds.ToDictionary(id => id, OutputFor);
    var execution = new Execution()
    {
        StartTime = DateTime.Now,
        Name = string.IsNullOrEmpty(restExecution.Name) ? null : restExecution.Name, 
        Outputs = [
            ..inputsToOutputs.Values
        ]
    };
    
    db.Executions.Add(execution);
    await db.SaveChangesAsync();

    foreach (var (inputId, output) in inputsToOutputs)
    {
        _ = Task.Run(() => ExecutionForInput(inputId, restExecution, output));
    }

    return;

    Output OutputFor(int inputId) => new()
    {
        InputId = inputId,
        Status = ExecutionStatus.Started,
        Components = []
    };
});

app.MapStaticAssets();
app.MapRazorPages()
    .WithStaticAssets();

app.Run();
return;

async Task ExecutionForInput(int inputId, RestExecution restExecution, Output output)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();

    dbContext.Attach(output);
    try
    {
        var input = dbContext.Inputs.Find(inputId) ?? throw new ArgumentException("Input not found");

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new(restExecution.Endpoint),
            Content = new ByteArrayContent(Convert.FromBase64String(input.OriginalRequest_Body))
            {
                Headers =
                {
                    {"SolidGroundOutputId",output.Id.ToString()},
                    {"Content-Type",input.OriginalRequest_ContentType}
                },
            }
        };
        
        httpClient.Timeout = TimeSpan.FromMinutes(10);
        var result = await httpClient.SendAsync(request);
        result.EnsureSuccessStatusCode();
    }
    catch (Exception ex)
    {
        output.Status = ExecutionStatus.Failed;
        output.Components.Add(new()
        {
            Name = "Error",
            Value = ex.ToString()
        });
        await dbContext.SaveChangesAsync();
    }
}

List<OutputComponent> OutputComponentsFromJsonElement(JsonElement jsonElement)
{
    return jsonElement
        .EnumerateObject()
        .Where(kvp => kvp.Value.ValueKind == JsonValueKind.String)
        .Select(kvp => new OutputComponent() { Name = kvp.Name, Value = kvp.Value.GetString() })
        .ToList();
}

async Task<Input> InputFor(JsonElement requestElement)
{
    var bodyBase64 = requestElement.GetRequired<string>("body_base64");
    var originalRequestContentType = requestElement.GetRequired<string>("content_type");
    var (inputFiles, inputStrings) = await ParseFormIntoStringsAndFiles(originalRequestContentType, bodyBase64);

    return new()
    {
        Files = inputFiles,
        Strings = inputStrings,
        OriginalRequest_ContentType = originalRequestContentType,
        OriginalRequest_Body = bodyBase64,
        OriginalRequest_Route = requestElement.GetRequired<string>("route"),
        OriginalRequest_Host = requestElement.GetRequired<string>("host"),
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


// record RestInput(string? Name, RestInputComponent[] Components);
// record RestInputComponent(ComponentType Type, string Data);

record RestExecution(int[] InputIds, string Endpoint, string? Name);

record RestPatchExecutionRequest(bool IsReference);
