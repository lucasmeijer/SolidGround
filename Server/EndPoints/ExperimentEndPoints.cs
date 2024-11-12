using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TurboFrames;

namespace SolidGround;

static class ExperimentEndPoints
{
    [SuppressMessage("ReSharper", "InconsistentNaming")] // ReSharper disable IdentifierTypo
    public static class Routes
    {
        public static readonly RouteTemplate api_experiment = RouteTemplate.Create("/api/experiment");
        public static readonly RouteTemplate api_experiment_newform = RouteTemplate.Create("/api/experiment/newform");
        public static readonly RouteTemplate api_experiment_newform_id = RouteTemplate.Create("/api/experiment/newform/{id:int}");
    }
    
    // public static void MapExperimentEndPoints(this IEndpointRouteBuilder app)
    // {
    //     app.MapGet(Routes.api_experiment_newform, () => new RunExperimentTurboFrame());
    //     app.MapGet(Routes.api_experiment_newform_id, (int id) => new RunExperimentTurboFrame(id));
    // }
}
//
// [ApiController]
// [Route("/api/experiment")]
// public class ExperimentController(AppDbContext db, IConfiguration config, IServiceProvider serviceProvider) : ControllerBase
// {
//     
//     
//     
//     [HttpPost]
//     public async Task<IActionResult> Post()
//     {
//         var form = await HttpContext.Request.ReadFormAsync();
//         if (!form.TryGetValue("ids", out var idValues))
//             return base.BadRequest("no ids specified");
//
//         var inputIds = JsonDocument.Parse(idValues.ToString())
//             .RootElement
//             .EnumerateArray()
//             .Select(o => o.GetInt32())
//             .ToArray();
//
//         inputIds = (await db.Inputs.ToArrayAsync()).Select(i => i.Id).ToArray();
//         var prefix = "SolidGroundVariable_";
//         var variables = form
//             .Where(kvp => kvp.Key.StartsWith(prefix))
//             .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());
//
//         var inputsToOutputs = inputIds.ToDictionary(id => id, OutputFor);
//
//         db.Executions.Add(new()
//         {
//             StartTime = DateTime.Now,
//             Outputs =
//             [
//                 ..inputsToOutputs.Values
//             ]
//         });
//         await db.SaveChangesAsync();
//         
//         var sb = new StringBuilder();
//         var input = await LastInput(db);
//
//         foreach (var (inputId, output) in inputsToOutputs)
//         {
//             sb.AppendLine($"""
//                            <turbo-stream action="replace" target="input_{inputId}">
//                            <template>
//                            <turbo-frame id="input_{inputId}" src="/api/input/{inputId}">
//                            </turbo-frame>
//                            </template>
//                            </turbo-stream>
//                            """);
//
//             var appEndPoint = $"{config.GetMandatory("SOLIDGROUND_TARGET_APP")}{input.OriginalRequest_Route}";
//
//             _ = Task.Run(() => ExecutionForInput(inputId, output, appEndPoint, variables));
//         }
//
//         return base.Content(sb.ToString(), "text/vnd.turbo-stream.html");
//
//         Output OutputFor(int inputId) => new()
//         {
//             InputId = inputId,
//             StringVariables =
//                 [..variables.Select(kvp => new StringVariable { Name = kvp.Key[prefix.Length..], Value = kvp.Value })],
//             Status = ExecutionStatus.Started,
//             Components = []
//         };
//
//     }
//     async Task<Input> LastInput(AppDbContext db)
//     {
//         return await db.Inputs.OrderByDescending(i => i.Id).FirstAsync() ?? throw new BadHttpRequestException("No inputs");
//     }
//
//     async Task ExecutionForInput(int inputId, Output output, string appEndPoint, Dictionary<string, string> variables)
//     {
//         using var scope = serviceProvider.CreateScope();
//         var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//         var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();
//
//         dbContext.Attach(output);
//         try
//         {
//             var input = dbContext.Inputs.Find(inputId) ?? throw new ArgumentException("Input not found");
//
//             Uri requestUri;
//
//             try
//             {
//                 requestUri = new Uri(appEndPoint);
//             }
//             catch (UriFormatException ufe)
//             {
//                 throw new BadHttpRequestException($"Invalid end point: {appEndPoint}", ufe);
//             }
//
//             var request = new HttpRequestMessage
//             {
//                 Method = HttpMethod.Post,
//                 RequestUri = requestUri,
//                 Content = new ByteArrayContent(Convert.FromBase64String(input.OriginalRequest_Body))
//                 {
//                     Headers =
//                     {
//                         { "SolidGroundOutputId", output.Id.ToString() },
//                         { "Content-Type", input.OriginalRequest_ContentType }
//                     },
//                 }
//             };
//
//             foreach (var variable in variables)
//                 request.Headers.Add(variable.Key, Convert.ToBase64String(Encoding.UTF8.GetBytes(variable.Value)));
//
//             httpClient.Timeout = TimeSpan.FromMinutes(10);
//
//             var result = await httpClient.SendAsync(request);
//             if (!result.IsSuccessStatusCode)
//             {
//                 var body = await result.Content.ReadAsStringAsync();
//                 output.Status = ExecutionStatus.Failed;
//                 output.Components.Add(new()
//                 {
//                     Name = $"Http Error {result.StatusCode}",
//                     Value = body
//                 });
//                 await dbContext.SaveChangesAsync();
//             }
//         }
//         catch (Exception ex)
//         {
//             output.Status = ExecutionStatus.Failed;
//             output.Components.Add(new()
//             {
//                 Name = "Error",
//                 Value = ex.ToString()
//             });
//             await dbContext.SaveChangesAsync();
//         }
//     }
// }