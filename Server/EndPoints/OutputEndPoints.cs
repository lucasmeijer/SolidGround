// using System.Diagnostics.CodeAnalysis;
// using System.Text.Json;
// using Microsoft.AspNetCore.Mvc;
// using TurboFrames;
//
// namespace SolidGround;
//
// static class OutputEndPoints
// {
//     [SuppressMessage("ReSharper", "InconsistentNaming")]
//     public static class Routes
//     {
//         public static readonly RouteTemplate api_output_id = RouteTemplate.Create("/api/outputs/{id:int}");
//     }
//     
//     public static void MapOutputEndPoints(this IEndpointRouteBuilder app)
//     {
//         app.MapDelete(Routes.api_output_id, async (AppDbContext db, int id) =>
//         {
//             var obj = await db.Outputs.FindAsync(id);
//             if (obj == null)
//                 return Results.NotFound();
//         
//             db.Outputs.Remove(obj);
//             await db.SaveChangesAsync();
//         
//             return new TurboStream("remove", Target: OutputTurboFrame.TurboFrameIdFor(id));
//         });
//         
//         app.MapPost(Routes.api_output_id, async (HttpRequest request, AppDbContext db, int id) =>
//         {
//             var jsonDoc = await JsonDocument.ParseAsync(request.Body);
//         
//             var output = await db.Outputs.FindAsync(id);
//             if (output == null)
//                 return Results.NotFound($"Output {id} not found");
//         
//             if (!jsonDoc.RootElement.TryGetProperty("outputs", out var outputElement))
//                 return Results.BadRequest("output element not found");
//         
//             output.Components = InputEndPoints.OutputComponentsFromJsonElement(outputElement);
//             output.Status = ExecutionStatus.Completed;
//             await db.SaveChangesAsync();
//             return Results.Ok();    
//         });
//     }
// }