using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using Mono.TextTemplating;
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
    }

    public static void MapExecutionsEndPoints(this IEndpointRouteBuilder app)
    {
        app.MapDelete(Routes.api_executions_id, async (int id, AppDbContext db) =>
        {
            var execution = await db.Executions.FindAsync(id) ?? throw new BadHttpRequestException("Execution with ID " + id + " not found.");
            await db.Entry(execution).Collection(e => e.Outputs).LoadAsync();
    
            db.Executions.Remove(execution);
            await db.SaveChangesAsync();

            return new TurboStreamCollection([
                ..execution.Outputs.Select(o => new TurboStream("remove", OutputTurboFrame.TurboFrameIdFor(o.Id)))
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
            
            var sb = new StringBuilder();
            foreach (var (inputId, output) in inputsToOutputs)
            {
                sb.AppendLine($"""
                               <turbo-stream action="replace" target="input_{inputId}">
                               <template>
                               <turbo-frame id="input_{inputId}" src="{InputEndPoints.Routes.api_input_id.For(inputId)}">
                               </turbo-frame>
                               </template>
                               </turbo-stream>
                               """);

                _ = Task.Run(() => ExecutionForInput(inputId, output.Id, runExecutionDto.EndPoint, runExecutionDto.StringVariables, serviceProvider));
            }

            return new TurboStreamCollection([..runExecutionDto.Inputs.Select(i => new TurboStream("replace",TurboFrameContent:new InputTurboFrame(i)))]);

            Output OutputFor(int inputId) => new()
            {
                InputId = inputId,
                StringVariables = [],
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