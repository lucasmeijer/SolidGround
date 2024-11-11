using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using SolidGroundClient;
using TurboFrames;

namespace SolidGround;

static class ExecutionsEndPoints
{
    [SuppressMessage("ReSharper", "InconsistentNaming")] // ReSharper disable IdentifierTypo
    public static class Routes
    {
        public static readonly RouteTemplate api_executions_id = RouteTemplate.Create("/api/executions/{id:int}");
        public static readonly RouteTemplate api_executions = RouteTemplate.Create("/api/executions");
        public static readonly RouteTemplate api_executions_new = RouteTemplate.Create("/api/executions/new");
        public static readonly RouteTemplate api_executions_new_production = RouteTemplate.Create("/api/executions/new/production");
        public static readonly RouteTemplate api_executions_new_executionid = RouteTemplate.Create("/api/executions/new/{executionId:int}");
        public static readonly RouteTemplate api_executions_id_name = RouteTemplate.Create("/api/executions/{id:int}/name");
        public static readonly RouteTemplate api_executions_id_name_edit = RouteTemplate.Create("/api/executions/{id:int}/name/edit");
    }

    public static void MapExecutionsEndPoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(Routes.api_executions_id_name, (int id) => new ExecutionNameTurboFrame(id, EditMode:false));
        app.MapGet(Routes.api_executions_id_name_edit, (int id) => new ExecutionNameTurboFrame(id, EditMode:true));
        app.MapGet(Routes.api_executions_new,() => new NewExecutionDialogContentTurboFrame());
        app.MapGet(Routes.api_executions_new_production, async (IConfiguration config, HttpClient httpClient, Tenant tenant) =>
        {
            var requestUri = $"{tenant.BaseUrl}/solidground";
            var routesArray = await httpClient.GetFromJsonAsync<JsonArray>(requestUri) ?? throw new Exception("No available variables found");
            var l = new List<StringVariableDto>();

            if (routesArray.Count == 0)
                return Results.BadRequest($"No routes found at {requestUri}");
            
            foreach (var x in routesArray[0]!["variables"]!.AsObject())
            {
                var valueObject = x.Value!.AsObject();
                string[] options = [];
                
                if (valueObject.TryGetPropertyValue("options", out var optionsNode) && optionsNode != null) 
                    options = optionsNode.AsArray().Select(e => e!.GetValue<string>()).ToArray();
                
                l.Add(new()
                {
                    Name = x.Key, 
                    Value = valueObject["value"]!.GetValue<string>(),
                    Options = options
                });
            }
            return new ExecutionVariablesTurboFrame([..l], "New execution");
        });
        app.MapGet(Routes.api_executions_new_executionid, async (int executionId, AppDbContext db) =>
        {
            var execution = await db.Executions
                .Include(e => e.Outputs)
                .ThenInclude(o=>o.StringVariables)
                .AsSplitQuery()
                .FirstOrDefaultAsync(e => e.Id == executionId)
                
                ?? throw new NotFoundException($"Execution {executionId} not found");

            var variables = execution
                .Outputs
                .First()
                .StringVariables
                .Select(s => new StringVariableDto()
                {
                    Name = s.Name, 
                    Value = s.Value, 
                    Options = []
                })
                .ToArray();
            
            return new ExecutionVariablesTurboFrame(variables, execution.Name+" Copy");
        });
        
        app.MapPost(Routes.api_executions_id_name, async (AppDbContext db, int id, InputEndPoints.NameUpdateDto nameUpdateDto) =>
        {
            var execution = await db.Executions.FindAsync(id);
            if (execution == null)
                return Results.NotFound($"Execution {id} not found");
            
            execution.Name = nameUpdateDto.Name;
            await db.SaveChangesAsync();

            return new ExecutionNameTurboFrame(id, EditMode:false);
        }).DisableAntiforgery();
        
        app.MapDelete(Routes.api_executions_id, async (int id, AppDbContext db) =>
        {
            var execution = await db.Executions.FindAsync(id) ?? throw new BadHttpRequestException("Execution with ID " + id + " not found.");
            await db.Entry(execution).Collection(e => e.Outputs).LoadAsync();
    
            db.Executions.Remove(execution);
            await db.SaveChangesAsync();

            return new TurboStreamCollection([
                ..execution.Outputs.Select(o => new TurboStream("remove", OutputTurboFrame.TurboFrameIdFor(o.Id))),
                new TurboStream("remove", ExecutionTurboFrame.TurboFrameIdFor(id))
            ]);
        });
        
        app.MapGet(Routes.api_executions_id, async (int id, AppDbContext db) =>
        {
            var execution = await db.Executions.FindAsync(id);
            
            if (execution == null)
                return Results.NotFound();
            
            await db.Entry(execution).Collection(e => e.Outputs).LoadAsync();
            var finished = execution.Outputs.All(o => o.Status != ExecutionStatus.Started);
            return Results.Json(new ExecutionStatusDto() { Finished = finished });
        });
        
        app.MapPost(Routes.api_executions, async (AppDbContext db, IConfiguration config, IServiceScopeFactory scopeFactory, RunExecutionDto runExecutionDto, HttpContext httpContext, AppState? appState) =>
        {
            var inputsToOutputs = runExecutionDto.Inputs.ToDictionary(id => id, OutputFor);

            var execution = new Execution()
            {
                StartTime = DateTime.Now,
                Outputs =
                [
                    ..inputsToOutputs.Values
                ],
                StringVariables = [..runExecutionDto.StringVariables.Select(s=> new StringVariable() { Name = s.Name, Value = s.Value})],
                SolidGroundInitiated = true
            };
            db.Executions.Add(execution);
            await db.SaveChangesAsync();

            foreach (var (inputId, output) in inputsToOutputs)
            {
                //todo: move to a background service that has logging on errors and telemetry
                _ = Task.Run(() => ExecutionForInput(inputId, output.Id, runExecutionDto.BaseUrl, runExecutionDto.StringVariables, scopeFactory));
            }

            if (!httpContext.Request.Headers.Accept.ToString().Contains("text/vnd.turbo-stream.html", StringComparison.OrdinalIgnoreCase))
                return Results.Accepted(Routes.api_executions_id.For(execution.Id));

            if (appState == null)
                throw new ArgumentNullException(nameof(appState));
            
            httpContext.Response.StatusCode = StatusCodes.Status202Accepted;
            return MorphedBodyUpdate.For(appState with { Executions = [..appState.Executions, execution.Id]});
            
            Output OutputFor(int inputId) => new()
            {
                InputId = inputId,
                StringVariables = [],
                Status = ExecutionStatus.Started,
                Components = []
            };
        });
    }
    
    
    static async Task ExecutionForInput(int inputId, int outputId, string baseUrl, StringVariableDto[] variables, IServiceScopeFactory scopeFactory)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();
        
        var output = await dbContext.Outputs.FindAsync(outputId) ?? throw new Exception("Output with ID " + outputId + " not found."); 
        try
        {
            var input = await dbContext.Inputs.FindAsync(inputId) ?? throw new ArgumentException("Input not found");

            Uri requestUri;

            try
            {
                requestUri = new Uri(baseUrl.TrimEnd("/") + input.OriginalRequest_Route + (input.OriginalRequest_QueryString ?? ""));
            }
            catch (UriFormatException ufe)
            {
                throw new BadHttpRequestException($"Invalid end point: {baseUrl}", ufe);
            }

            var request = new HttpRequestMessage
            {
                Method = string.Equals(input.OriginalRequest_Method, "post", StringComparison.InvariantCultureIgnoreCase) ? HttpMethod.Post : HttpMethod.Get,
                RequestUri = requestUri,
                Content = new ByteArrayContent(Convert.FromBase64String(input.OriginalRequest_Body))
                {
                    Headers = 
                    {
                        Headers(),
                    },
                }
            };

            IEnumerable<KeyValuePair<string, string>> Headers()
            {
                yield return new(SolidGroundConstants.SolidGroundOutputId, outputId.ToString());
                if (input.OriginalRequest_ContentType != null)
                    yield return new("Content-Type", input.OriginalRequest_ContentType);
                foreach (var variable in variables)
                    yield return new($"{SolidGroundConstants.HeaderVariablePrefix}{variable.Name}", Convert.ToBase64String(Encoding.UTF8.GetBytes(variable.Value)));
            }
            
            httpClient.Timeout = TimeSpan.FromMinutes(10);

            var result = await httpClient.SendAsync(request);
            if (!result.IsSuccessStatusCode)
            {
                var body = await result.Content.ReadAsStringAsync();
                output.Status = ExecutionStatus.Failed;
                output.Components.Add(new()
                {
                    Name = $"Http Error {result.StatusCode}",
                    Value = body
                });
                await dbContext.SaveChangesAsync();
            }
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
}