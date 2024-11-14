using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
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
        app.MapGet(Routes.api_executions_new_production, async (IWebHostEnvironment env, HttpClient httpClient, Tenant tenant) =>
        {
            var l = await StringVariableDtosFromProduction(tenant, httpClient, env.IsDevelopment());
            return new ExecutionVariablesTurboFrame([..l], "New execution");
        });
        app.MapGet(Routes.api_executions_new_executionid, async (int executionId, AppDbContext db,HttpClient httpClient, Tenant tenant, IWebHostEnvironment env) =>
        {
            var execution = await db.Executions
                .Include(e => e.Outputs)
                .ThenInclude(o=>o.StringVariables)
                .AsSplitQuery()
                .FirstOrDefaultAsync(e => e.Id == executionId)
                
                ?? throw new NotFoundException($"Execution {executionId} not found");

            var productionVariables = await StringVariableDtosFromProduction(tenant, httpClient, env.IsDevelopment());

            var outputVariables = execution
                .Outputs
                .First()
                .StringVariables;
            
            var patched = productionVariables.Select(p =>
            {
                var outputVar = outputVariables.SingleOrDefault(o => o.Name == p.Name);
                if (outputVar == null)
                    return p;
                return p with { Value = outputVar.Value };
            });
            
            return new ExecutionVariablesTurboFrame([..patched], execution.Name+" Copy");
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
        
        app.MapPost(Routes.api_executions, async (AppDbContext db, RunExecutionDto runExecutionDto, HttpContext httpContext, AppState? appState, BackgroundWorkService backgroundWorkService, Tenant tenant) =>
        {
            var inputsToOutputs = runExecutionDto.Inputs.ToDictionary(id => id, OutputsFor);
            
            var execution = new Execution()
            {
                Name = string.IsNullOrWhiteSpace(runExecutionDto.Name) ? null : runExecutionDto.Name,
                StartTime = DateTime.Now,
                Outputs =
                [
                    ..inputsToOutputs.Values.SelectMany(o => o)
                ],
                StringVariables = [..runExecutionDto.StringVariables.Select(s=> new StringVariable()
                {
                    Name = s.Name, 
                    Value = s.Value,
                })],
                SolidGroundInitiated = true
            };
            db.Executions.Add(execution);
            await db.SaveChangesAsync();

            foreach (var (inputId, outputs) in inputsToOutputs)
            {
                var input = await db.Inputs.FindAsync(inputId) ?? throw new ArgumentException("Input not found");

                foreach (var output in outputs)
                {
                    var request = RequestFor(runExecutionDto, input);
                    await backgroundWorkService.QueueWorkAsync(async (serviceProvider, cancellationToken) =>
                    {
                        await SendRequestAndProcessResponse(serviceProvider, request, output.Id, cancellationToken, tenant);
                    });
                }
            }

            if (!httpContext.Request.Headers.Accept.ToString().Contains("text/vnd.turbo-stream.html", StringComparison.OrdinalIgnoreCase))
                return Results.Accepted(Routes.api_executions_id.For(execution.Id));

            if (appState == null)
                throw new ArgumentNullException(nameof(appState));
            
            httpContext.Response.StatusCode = StatusCodes.Status202Accepted;
            return MorphedBodyUpdate.For(appState with { Executions = [..appState.Executions, execution.Id]});
            
            Output[] OutputsFor(int inputId) => Enumerable.Range(0, runExecutionDto.RunAmount).Select(_ => new Output()
            {
                InputId = inputId,
                StringVariables = [],
                Status = ExecutionStatus.Started,
                Components = []
            }).ToArray();
        });
        
        static async Task SendRequestAndProcessResponse(IServiceProvider sp, HttpRequestMessage request, int outputId, CancellationToken ct, Tenant tenant)
        {
            
            var httpClient = sp.GetRequiredService<HttpClient>();
            httpClient.Timeout = TimeSpan.FromMinutes(10);
            var result = await httpClient.SendAsync(request, ct);
            
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            sp.GetRequiredService<IDatabaseConfigurationForTenant>().Configure(optionsBuilder, tenant);
            await using var dbContext = new AppDbContext(optionsBuilder.Options);

            var output = await dbContext.Outputs.FindAsync([outputId], ct) ?? throw new Exception("Output with ID " + outputId + " not found.");

            await PopulateOutput(output, result,ct);
            await dbContext.SaveChangesAsync(ct);
        }

        HttpRequestMessage RequestFor(RunExecutionDto runExecutionDto, Input input) => new()
        {
            Method = string.Equals(input.OriginalRequest_Method, "post",
                StringComparison.InvariantCultureIgnoreCase)
                ? HttpMethod.Post
                : HttpMethod.Get,
            RequestUri = RequestUrlFor(runExecutionDto.BaseUrl, input),
            Content = new ByteArrayContent(Convert.FromBase64String(input.OriginalRequest_Body))
            {
                Headers =
                {
                    (KeyValuePair<string, string>[])
                    [
                        new(SolidGroundConstants.SolidGroundInitiated, "1"),
                        ..runExecutionDto.StringVariables.Select(HeaderFor),
                    ],

                    input.OriginalRequest_ContentType != null
                        ? [new("Content-Type", input.OriginalRequest_ContentType)]
                        : [],
                },
            }
        };
    }

    static KeyValuePair<string, string> HeaderFor(StringVariableDto variable)
    {
        return new($"{SolidGroundConstants.HeaderVariablePrefix}{variable.Name}", Convert.ToBase64String(Encoding.UTF8.GetBytes(variable.Value)));
    }

    static async Task PopulateOutput(Output output, HttpResponseMessage result, CancellationToken cancellationToken)
    {
        var resultContent = await result.Content.ReadAsStringAsync(cancellationToken);

        if (!result.IsSuccessStatusCode)
        {
            PopulateAsFailure(resultContent);
            return;
        }

        try
        {
            var outputDto = JsonSerializer.Deserialize<OutputDto>(resultContent);
            if (outputDto != null)
            {
                var outputObject = InputEndPoints.OutputFor(outputDto, null);
                output.Components = outputObject.Components;
                output.StringVariables = outputObject.StringVariables;
                output.Status = ExecutionStatus.Completed;
                return;
            }
        } catch(Exception e)
        {
            PopulateAsFailure(e.ToString());    
        }
        return;

        void PopulateAsFailure(string content)
        {
            output.Status = ExecutionStatus.Failed;
            output.Components =
            [
                new OutputComponent()
                {
                    Name = $"Http Error {result.StatusCode}",
                    Value = content,
                    ContentType = "text/plain",
                }
            ];
        }
    }

    static async Task<StringVariableDto[]> StringVariableDtosFromProduction(Tenant tenant, HttpClient httpClient, bool isDevelopment)
    {
        var requestUrl = $"{(isDevelopment ? tenant.LocalBaseUrl : tenant.BaseUrl)}/solidground";
        var routesArray = await httpClient.GetFromJsonAsync<JsonArray>(requestUrl) ?? throw new Exception("No available variables found");
        var l = new List<StringVariableDto>();

        if (routesArray.Count == 0)
            throw new ResultException(Results.BadRequest($"No routes found at {requestUrl}"));
            
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

        return [..l];
    }

    static Uri RequestUrlFor(string baseUrl, Input input)
    {
        Uri requestUri;
        try
        {
            requestUri = new Uri(baseUrl.TrimEnd('/') + input.OriginalRequest_Route + (input.OriginalRequest_QueryString ?? ""));
        }
        catch (UriFormatException ufe)
        {
            throw new BadHttpRequestException($"Invalid end point: {baseUrl}", ufe);
        }

        return requestUri;
    }
}
