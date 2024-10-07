using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SolidGround;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite("Data Source=solid_ground.db"));

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



app.MapPost("/api/inputs", async (InputsParams input, AppDbContext db) =>
{
    // Validate input
    if (string.IsNullOrEmpty(input.AppName) || string.IsNullOrEmpty(input.Data))
    {
        return Results.BadRequest("AppName and Data are required.");
    }

    var newInput = new Input
    {
        Name = "kleine kapitein",
        Components = [ new InputComponent()
        {
            Type = ComponentType.String,
            StringValue = "er was eens een kapitein, die heel veel pannekoeken at",
        }]
    };

    db.Inputs.Add(newInput);
    await db.SaveChangesAsync();

    return Results.Created($"/api/inputs/{newInput.Id}", newInput);
});




app.MapStaticAssets();
app.MapRazorPages()
    .WithStaticAssets();

app.Run();