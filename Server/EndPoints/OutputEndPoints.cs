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
            if (output.StringVariables.Count > 0)
            {
                foreach (var variable in output.StringVariables) 
                    v[variable.Name] = variable.Value;
            }
            else
            {
                foreach (var dto in await ExecutionsEndPoints.StringVariableDtosFromProduction(tenant, httpClient, env.IsDevelopment()))
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


            prompt.AppendLine("""
                              <application_information>
                              The application is a writing assistant for psychologists that write assessment reports.
                              
                              A company hires our psychologist to either decide if a certain candidate should get a job (selectie advies),
                              or hires the psychologist to write a recommendation for how the person can develop themselves to be suitable for a certain role
                              (ontwikkeladvies).
                              
                              The applications job is to write the psychologist report, based on the following inputs:
                              
                              inputs to the application are 
                              - 'functieomschrijving': which is a description of the job to be filled
                              - 'compenties': which are the result of a skill test the psychologist had the candidate do. the candidate
                              gets scored on several different competenties that are relevant to the job.
                              - recap: these are notes the psychologist has written down after the interview has taken place. consider these as instructions
                              on what should be in the rapport.
                              - passages: wether this is a selectie & ontwikkeladvies, or only a ontwikkeladvies.
                              
                              There's different example rapports for the two kinds of rapports.
                              
                              All prompts for this application should be written in dutch.
                              </application_information>

                              <prompting_guidelines>
                              The application itself expects the final written rapport to be written inside <rapport></rapport> tags. we can use everything before that as a scratch pad.
                              The LLM used by the application works well with chain of thought prompting. If you want to make sure the final report contains something
                              you can insert in the chain of thought prompt a request to write that something in <something></something> tags. The fact that the LLM has to write it out
                              makes it more likely to "remember" that fact when it is writing the actual prompt.
                              
                              The general structure of a good prompt is:
                              
                              - a system prompt that does a role based assignment to set the stage. "You are an expert psychologist with a very clear writing style".
                              
                              a regular prompt that:
                              - describes who this application is for, and what they want it to do.
                              - starts with a description of what the inputs of the application are.
                              - a description of what the outputs of the applications are.
                              
                              - a chain-of-thought list of steps that must be taken to come to a good output.
                              - the final instruction of the chain of thought should be to write a rapport in <rapport</rapport> tags, and specify the language it needs to be written in.
                              </prompting_guidelines>
                              
                              You will be assisting in optimizing an AI application. 
                              You'll find one or more <execution_?> tags. these contain input/output pairs for this application that it has produced in the past.
                              You'll also find userfeedback in it. That contains feedback the end user has about that output of the application.
                              You'll also find <ai_variables></ai_variables>. These contain the values of the configurable settings the application has, at the time this
                              input/output pair was produced.
                              
                              You might be asked to suggest changes to the ai_variables. The application will take the ai_variables, and produce a large language model prompt from them.
                              It does so in a straight forward manner. The prompt template itself is a variable, when you see **SOMETHING** that means the application will inject something
                              into the prompt there. When changing ai variables related to the prompting, take into consideration the <prompting_guidelines></prompting_guidelines> you need
                              to adhere to. Refer to <application_info> to better understand what the application should do, and the context in which it will be used.
                              
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