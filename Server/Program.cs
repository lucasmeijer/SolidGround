using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SolidGround;
using SolidGround.Pages;
using TurboFrames;
[assembly: InternalsVisibleTo("Tests")]

var builder = WebApplication.CreateBuilder(args);

var persistentStorage = builder.Configuration["PERSISTENT_STORAGE"] ?? ".";
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite($"Data Source={persistentStorage}/solid_ground.db"));
builder.Services.AddHttpClient();
builder.Services.AddHealthChecks().AddCheck("Health", () => HealthCheckResult.Healthy("OK"));
builder.Services.AddControllersWithViews();
builder.Services.AddControllers();
var app = builder.Build();
app.MapControllers();

{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<AppDbContext>();
    if (dbContext.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
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

    return new TurboStreamCollection([
        ..execution.Outputs.Select(o => new TurboStream("remove", OutputTurboFrame.TurboFrameIdFor(o.Id)))
    ]);
});


//app.MapStaticAssets();
app.UseStaticFiles();

//    .WithStaticAssets();

app.MapGet("/", () => new IndexPage());
app.MapGet("/tags", () => new TagsPage()).WithName("tags");

app.MapTagsEndPoints();
app.MapSearchEndPoints();
app.MapOutputEndPoints();
app.MapInputEndPoints();
app.MapExperimentEndPoints();

app.Run();

public static class TagHelper
{
    public static async Task<Tag> FindTag(JsonElement tagidElement, AppDbContext appDbContext)
    {
        if (tagidElement.ValueKind != JsonValueKind.Number)
            throw new BadHttpRequestException("Tag not a number");

        var tagid = tagidElement.GetInt32();
        var t = await appDbContext.Tags.FindAsync(tagid);
        if (t == null)
            throw new BadHttpRequestException("Tag not found");
        return t;
    }
}

public partial class Program { }