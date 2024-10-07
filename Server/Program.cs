using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SolidGround;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite("Data Source=solid_ground.db"));
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

app.MapPost("/api/inputs", async (RestInput restInput, AppDbContext db) =>
{
    InputComponent InputComponentFor(RestInputComponent ric) => new()
    {
        Type = ric.Type,
        BinaryValue = ric.Type is ComponentType.File or ComponentType.Image
            ? Convert.FromBase64String(ric.Data)
            : null,
        StringValue = ric.Type is ComponentType.String ? ric.Data : null,
    };

    var newInput = new Input
    {
        Name = restInput.Name,
        Components = [..restInput.Components.Select(InputComponentFor)]
    };

    db.Inputs.Add(newInput);
    await db.SaveChangesAsync();

    return Results.Created($"/api/inputs/{newInput.Id}", null);
});

app.MapPost("/api/execution", async (RestExecution restExecution, AppDbContext db, HttpClient httpClient) =>
{
    var inputsToOutputs = restExecution.InputIds.ToDictionary(id => id, OutputFor);
    var execution = new Execution()
    {
        StartTime = DateTime.Now,
        Name = restExecution.Name, 
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

async Task Go2(HttpClient httpClient, RestExecution restExecution, Output output, Input Input)
{
    ApplyResult(await GetResult(), output);
    return;

    async Task<string> GetResult()
    {
        bool dummy = true;
        if (dummy)
        {
            return """
                   {
                   "prompt": "Yeah it was a good prompt",
                   "outcome1": "it was good"
                   }
                   """;
        }

        var result = await httpClient.PostAsync(restExecution.Endpoint, JsonContent.Create($"{Input.Id}"));
        result.EnsureSuccessStatusCode();
        return await result.Content.ReadAsStringAsync();
    }

    static void ApplyResult(string result, Output output)
    {
        var json = JsonDocument.Parse(result);

        output.Components = [ ..json.RootElement
            .EnumerateObject()
            .Select(kvp => new OutputComponent
            {
                Name = kvp.Name, 
                Value = kvp.Value.GetString()
            }) 
        ];

        output.Status = ExecutionStatus.Completed;
    }
}

async Task ExecutionForInput(int inputId, RestExecution restExecution, Output output)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();
    try
    {
        await Go2(httpClient, restExecution, output, dbContext.Inputs.Find(inputId) ?? throw new ArgumentException("Input not found"));
    }
    catch (Exception ex)
    {
        output.Status = ExecutionStatus.Failed;
        output.Components.Add(new()
        {
            Name = "Error",
            Value = ex.ToString()
        });
    }

    dbContext.Update(output);
    await dbContext.SaveChangesAsync();
}

record RestInput(string? Name, RestInputComponent[] Components);
record RestInputComponent(ComponentType Type, string Data);
record RestExecution(int[] InputIds, string Endpoint, string Name);