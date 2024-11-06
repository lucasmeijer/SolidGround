using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SolidGroundClient;
using TurboFrames;

namespace SolidGround;

public static class ExecutionsEndPoints
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
        app.MapGet(Routes.api_executions_new_production, async (IConfiguration config, HttpClient httpClient) =>
        {
            var requestUri = $"{ExecutionVariablesTurboFrame.TargetAppBaseAddress(config)}/solidground";
            var availableVariablesDto = await httpClient.GetFromJsonAsync<AvailableVariablesDto>(requestUri) ?? throw new Exception("No available variables found");
            return new ExecutionVariablesTurboFrame(availableVariablesDto.StringVariables, "New execution");
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
                .Select(s => new StringVariableDto() { Name = s.Name, Value = s.Value })
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
        
        app.MapPost(Routes.api_executions, async (AppDbContext db, IConfiguration config, IServiceProvider serviceProvider, RunExecutionDto runExecutionDto, HttpContext httpContext) =>
        {
            var inputsToOutputs = runExecutionDto.Inputs.ToDictionary(id => id, OutputFor);

            var execution = new Execution()
            {
                StartTime = DateTime.Now,
                Outputs =
                [
                    ..inputsToOutputs.Values
                ]
            };
            db.Executions.Add(execution);
            await db.SaveChangesAsync();
            httpContext.Response.StatusCode = StatusCodes.Status201Created;
            httpContext.Response.Headers.Location = Routes.api_executions_id.For(execution.Id);
            
            foreach (var (inputId, output) in inputsToOutputs)
            {
                _ = Task.Run(() => ExecutionForInput(inputId, output.Id, runExecutionDto.EndPoint, runExecutionDto.StringVariables, serviceProvider));
            }

            var all = await db.Executions.Select(e => e.Id).ToArrayAsync(); 
            return new TurboStreamCollection([
                ..runExecutionDto.Inputs.Select(i => new TurboStream("replace",TurboFrameContent:new InputTurboFrame(i, all), Method: "morph")),
                new TurboStream("replace", TurboFrameContent:new FilterBarExecutionsList(), Method: "morph")
            ]);

            Output OutputFor(int inputId) => new()
            {
                InputId = inputId,
                StringVariables = [..runExecutionDto.StringVariables.Select(s=> new StringVariable() { Name = s.Name, Value = s.Value})],
                Status = ExecutionStatus.Started,
                Components = []
            };
        });
    }
    
    
    static async Task ExecutionForInput(int inputId, int outputId, string appEndPoint, StringVariableDto[] variables, IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();
        
        var output = await dbContext.Outputs.FindAsync(outputId) ?? throw new Exception("Output with ID " + outputId + " not found."); 
        try
        {
            var input = await dbContext.Inputs.FindAsync(inputId) ?? throw new ArgumentException("Input not found");

            Uri requestUri;

            try
            {
                requestUri = new Uri(appEndPoint);
            }
            catch (UriFormatException ufe)
            {
                throw new BadHttpRequestException($"Invalid end point: {appEndPoint}", ufe);
            }

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
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