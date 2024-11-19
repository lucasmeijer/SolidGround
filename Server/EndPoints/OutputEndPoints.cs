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

        app.MapGet(Routes.api_output_id_prompt, async (int id, AppDbContext db,HttpClient httpClient, Tenant tenant, IWebHostEnvironment env) =>
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
            var solidGroundRouteInfo = await ExecutionsEndPoints.ClientInfoFor(tenant, httpClient, env.IsDevelopment());
            if (output.StringVariables.Count > 0)
            {
                foreach (var variable in output.StringVariables) 
                    v[variable.Name] = variable.Value;
            }
            else
            {
                //temp path to deal with items already in the database that didn't store variables.
                foreach (var dto in solidGroundRouteInfo.StringVariables)
                    v[dto.Name] = dto.Value;
            }
            
            var options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            prompt.AppendLine(JsonSerializer.Serialize(v, options));
            prompt.AppendLine("</ai_variables>");
            prompt.AppendLine("<input>");

            var input = db.Inputs.Include(i => i.Strings).First(i => i.Id == output.InputId);

            if (input.OriginalRequest_ContentType == "application/json")
            {
                var uglyJsonString = Encoding.UTF8.GetString(Convert.FromBase64String(input.OriginalRequest_Body));
                var o = JsonSerializer.Deserialize(uglyJsonString, typeof(object), options);
                prompt.AppendLine(JsonSerializer.Serialize(o, options));
            }
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
            prompt.AppendLine("<application_information>");
            prompt.AppendLine(solidGroundRouteInfo.ApplicationInformation);
            prompt.AppendLine("</application_information>");
            prompt.AppendLine("<prompting_guidelines>");
            prompt.AppendLine(solidGroundRouteInfo.PromptingGuidelines);
            prompt.AppendLine("</prompting_guidelines>");
            prompt.AppendLine("""
                              You will be assisting in optimizing an AI application. 
                              You'll find one or more <execution_?> tags. these contain input/output pairs for this application that it has produced in the past.
                              You might also find userfeedback in it. That contains feedback the end user has about that output of the application.
                              You'll also find <ai_variables></ai_variables>. These contain the values of the configurable settings the application has, at the time this
                              input/output pair was produced.
                              
                              You might be asked to suggest changes to the ai_variables. The application will take the ai_variables, and produce a large language model prompt from them.
                              It does so in a straight forward manner. The prompt template itself is a variable, when you see **SOMETHING** that means the application will inject something
                              into the prompt there. When changing ai variables related to the prompting, take into consideration the <prompting_guidelines></prompting_guidelines> you need
                              to adhere to. Refer to <application_information> to better understand what the application should do, and the context in which it will be used.
                              
                              When you are asked to make suggestions for different values of ai_variables that we could try to get better results, make minimal changes.
                              do not change all values for the fun of it. First think out loud of the difference in behaviour you'd like to see, then make a plan on which ai_variable(s) to change
                              for that,  and then only make that decisive change, keeping all other variables untouched. only write the new values of variables you actually changed.
                              """);
            
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