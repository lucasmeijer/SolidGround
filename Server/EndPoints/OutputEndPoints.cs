using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Unicode;
using Microsoft.EntityFrameworkCore;
using TurboFrames;

namespace SolidGround;

static class OutputEndPoints
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class Routes
    {
        public static readonly RouteTemplate api_output_id = RouteTemplate.Create("/api/outputs/{id:int}");
        public static readonly RouteTemplate api_output_id_prompt = RouteTemplate.Create("/api/outputs/{id:int}/prompt");
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

        app.MapGet(Routes.api_output_id_prompt, async (int id, AppDbContext db) =>
        {
            var output = await db.Outputs
                .Include(o => o.StringVariables)
                .Include(output => output.Components).Include(output => output.Execution)
                .ThenInclude(execution => execution.StringVariables)
                .AsSplitQuery()
                .FirstOrDefaultAsync(o => o.Id == id);

            if (output == null)
                return Results.NotFound();

            var result = output.Components.FirstOrDefault(c => c.Name == "result");
            if (result == null)
                return Results.NotFound();

            var prompt = new StringBuilder();

            prompt.AppendLine($"<execution_{output.Id}>");
            prompt.AppendLine("<ai_variables>");
            var v = new JsonObject();
            foreach (var variable in output.StringVariables) 
                v[variable.Name] = variable.Value;
            
            var options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            prompt.AppendLine(JsonSerializer.Serialize(v, options));
            prompt.AppendLine("</ai_variables>");
            prompt.AppendLine("<input>");

            var input = db.Inputs.Include(i => i.Strings).First(i => i.Id == output.InputId);

            if (input.OriginalRequest_ContentType == "application/json")
                prompt.AppendLine(Encoding.UTF8.GetString(Convert.FromBase64String(input.OriginalRequest_Body)));
            else 
                throw new ArgumentException("This input does not have json original request");
            
            prompt.AppendLine("</input>");

            prompt.AppendLine("<output>");
            prompt.AppendLine(result.Value);
            prompt.AppendLine("</output>");

            var feedbackComponent = output.Components.FirstOrDefault(c => c.Name == "UserFeedback");
            if (feedbackComponent != null)
            {
                prompt.AppendLine("<userfeedback>");
                prompt.AppendLine(feedbackComponent.Value);
                prompt.AppendLine("</userfeedback>");
            }

            prompt.AppendLine($"</execution_{output.Id}>");

            return Results.Text(prompt.ToString());
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