using System.Diagnostics.CodeAnalysis;
using TurboFrames;

namespace SolidGround;

public static class ExecutionsEndPoints
{
    [SuppressMessage("ReSharper", "InconsistentNaming")] // ReSharper disable IdentifierTypo
    public static class Routes
    {
        public static readonly RouteTemplate api_executions_id = RouteTemplate.Create("/api/executions/{id:int}");
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
    }
}