using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TurboFrames;

namespace SolidGround;

static class OutputEndPoints
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class Routes
    {
        public static readonly RouteTemplate api_output_id = RouteTemplate.Create("/api/outputs/{id:int}");
    }
    
    public static void MapOutputEndPoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(Routes.api_output_id, (int id) => new OutputTurboFrame(id, true));
        
        app.MapDelete(Routes.api_output_id, async (AppDbContext db, int id) =>
        {
            var obj = await db.Outputs.FindAsync(id);
            if (obj == null)
                return Results.NotFound();
        
            db.Outputs.Remove(obj);
            await db.SaveChangesAsync();
        
            return new TurboStream("remove", Target: OutputTurboFrame.TurboFrameIdFor(id));
        });
        //
        // app.MapPatch(Routes.api_output_id, async (AppDbContext db, int id, OutputDto outputDto) =>
        // {
        //     var output = await db.Outputs.FindAsync(id);
        //     if (output == null)
        //         return Results.NotFound($"Output {id} not found.");
        //     var outputObject = InputEndPoints.OutputFor(outputDto, null);
        //     output.Components = outputObject.Components;
        //     output.StringVariables = outputObject.StringVariables;
        //     output.Status = ExecutionStatus.Completed;
        //     await db.SaveChangesAsync();
        //     return Results.Ok();
        // }).RequireTenantApiKey();
    }
}