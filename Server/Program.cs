using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
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



//app.MapStaticAssets();
app.UseStaticFiles();

//    .WithStaticAssets();

app.MapGet("/", () => new IndexPage());


app.MapTagsEndPoints();
app.MapSearchEndPoints();
app.MapInputEndPoints();
app.MapExperimentEndPoints();
app.MapImagesEndPoints();
app.MapExecutionsEndPoints();

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