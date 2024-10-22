using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Net.Http.Headers;
using SolidGround;
using TurboFrames;

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
app.MapTurboFramesInSameAssemblyAs(typeof(Program));

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

app.MapPost("/api/search", async (AppDbContext db, HttpRequest request) =>
{
    var json = (await JsonDocument.ParseAsync(request.Body)).RootElement;
    if (!json.TryGetProperty("tags", out var tagsElement))
        throw new BadHttpRequestException("no tags found");
    
    var tags = await Task.WhenAll(tagsElement.EnumerateArray().Select(async t => await FindTag(t, db)));
    
    if (!json.TryGetProperty("search", out var searchElement))
        throw new BadHttpRequestException("no search element found");

    if (!json.TryGetProperty("tags_changed", out var tagsChangedElement))
        throw new BadHttpRequestException("no tags changed");
    var tagsChanged = tagsChangedElement.GetBoolean();
    
    var searchString = searchElement.GetString()?.Trim();

    var searchTagsIds = tags
        .Select(t=>t.Id)
        .ToArray();

    var queryable = db.Inputs
        .Include(i => i.Tags)
        .Where(i => searchTagsIds.All(searchTagId => i.Tags.Any(it => it.Id == searchTagId)));

    if (!string.IsNullOrEmpty(searchString))
        queryable = queryable.Where(i => i.Name!.Contains(searchString));

    return new TurboStreams2([
        new("replace", TurboFrameContent: new InputList(await queryable.Select(t => t.Id).ToArrayAsync())),
        ..tagsChanged ? 
            [new("replace", TurboFrameContent: new FilterBarTurboFrame(tags))] 
            : Array.Empty<TurboStream>()
    ]);
    
}).DisableAntiforgery();

async Task<Tag> FindTag(JsonElement tagidElement, AppDbContext appDbContext)
{
    if (tagidElement.ValueKind != JsonValueKind.Number)
        throw new BadHttpRequestException("Tag not a number");

    var tagid = tagidElement.GetInt32();
    var t = await appDbContext.Tags.FindAsync(tagid);
    if (t == null)
        throw new BadHttpRequestException("Tag not found");
    return t;
}

app.MapPost("/api/input/{inputId}/tags", async (int inputId, AppDbContext db, [FromForm] string tagData) =>
{
    var input = await db.Inputs
        .Include(i=>i.Tags)
        .FirstOrDefaultAsync(i=>i.Id == inputId);
    if (input == null)
        return Results.BadRequest($"Input {inputId} not found");

    var json = JsonDocument.Parse(tagData).RootElement;
    
    if (json.TryGetProperty("add_tag", out var tagToAddElement))
    {
        input.Tags.Add(await FindTag(tagToAddElement, db));
        await db.SaveChangesAsync();
    }

    if (json.TryGetProperty("remove_tag", out var tagToRemoveElement))
    {
        var find = await FindTag(tagToRemoveElement, db);
        input.Tags.Remove(find);
        await db.SaveChangesAsync();
    }
    
    return new InputTagsTurboFrame(inputId);
}).DisableAntiforgery();



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
