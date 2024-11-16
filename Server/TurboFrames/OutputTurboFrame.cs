using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using SolidGround.Pages;
using TurboFrames;

namespace SolidGround;

record OutputTurboFrame(int OutputId, bool StartOpened) : TurboFrame(TurboFrameIdFor(OutputId))
{
    public static string TurboFrameIdFor(int outputId) => $"output_{outputId}";
    protected override bool SkipTurboFrameTags => true;

    protected override string LazySrc => OutputEndPoints.Routes.api_output_id.For(OutputId);

    protected override Delegate RenderFunc => async (AppDbContext db) =>
    {
        var output = await db.Outputs
            .Include(o => o.Components)
            .Include(o => o.Execution)
            .Include(o => o.StringVariables)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.Id == OutputId) 
                                  ?? throw new ResultException(Results.NotFound($"Output {OutputId} not found"));

        bool finished = output.Status != ExecutionStatus.Started;

        var spinner = new Html("""
                                     <div class="animate-spin rounded-full h-8 w-8 border-t-4 border-b-4 border-blue-500"></div>
                               """);
        return new Html($"""
                         <turbo-frame data-src="{OutputEndPoints.Routes.api_output_id.For(OutputId)}" id="{TurboFrameId}" class="flex-1 w-0" {(finished ? "" : "data-controller='autoreload' data-turbo-cache='false'")}>
                         <div class="flex flex-row gap-2 items-stretch">
                             <details class="bg-gray-50 flex-1 shadow-md rounded-lg group/output {ColorFor(output)}" {(StartOpened ? "open" : "")}>
                                 <summary class="p-4 cursor-pointer flex justify-between items-center rounded-lg ">
                                     {output.Execution.Name ?? "Naamloos"}
                                     {(finished ? "" : spinner)}
                                     
                                     
                                     <svg class="w-5 h-5 transition-transform duration-200 group-open/output:rotate-90" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                                         <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"></path>
                                     </svg>
                                 </summary>
                                 <div class="p-2">
                                     <div class="flex gap-2">
                                         {ResultHtmlsFor(output).Render()}
                                     </div>
                                     {(output.Cost == null ? "Cost unknown" : $"{1m/output.Cost:F}/$")}
                                     <details class="my-4">    
                                         <summary class="text-sm">Details</summary>
                                         <div class="p-4">
                                             {output.Components.Where(c => c.Name != "result").Render(RenderComponent)}
                                             <br/>
                                             <br/>
                                             <br/>
                                             {output.StringVariables.Render(RenderStringVariable)}
                                         </div>
                                     </details>
                                 </div>
                             </details>

                         </div>
                         </turbo-frame>
                         """);
    };

    static Html[] ResultHtmlsFor(Output output)
    {
        var result = output.Components.FirstOrDefault(c => c.Name == "result");
        return result == null 
            ? [] 
            : [new($"""
                    <div class="prose">
                        {RenderValue(result)}
                    </div>
                    """)];
    }

    static Html RenderStringVariable(StringVariable stringVariable) => new($"""
        <details class="p-2 border-b last:border-b-0 text-sm group/component">
            <summary class="cursor-pointer flex justify-between items-center hover:bg-gray-50">
                <h3 class="font-semibold">{stringVariable.Name}</h3>
                <svg class="w-5 h-5 transition-transform duration-200 transform rotate-0 group-open/component:rotate-90" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"></path>
                </svg>
            </summary>
            <div class="py-2 text-xs">
                {JsonFormatter.FormatMaybeJson(stringVariable.Value)}
            </div>
        </details>
        """);

    static Html RenderComponent(OutputComponent c) => new($"""
               <details class="p-2 border-b last:border-b-0 text-sm group/component">
                   <summary class="cursor-pointer flex justify-between items-center hover:bg-gray-50">
                       <h3 class="font-semibold">{c.Name}</h3>
                       <svg class="w-5 h-5 transition-transform duration-200 transform rotate-0 group-open/component:rotate-90" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                           <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"></path>
                       </svg>
                   </summary>
                   <div class="py-2 text-xs">
                       {RenderValue(c)}
                   </div>
               </details>
           """);

    static Html RenderValue(OutputComponent c)
    {
        return c.ContentType == "application/html" ? new($"""
                                                                     <div class="html-highlight list-disc list-inside">
                                                                     {c.Value}
                                                                     </div>
                                                                     """) : JsonFormatter.FormatMaybeJson(c.Value);
    }

    static string ColorFor(Output output) => output.Status switch
    {
        ExecutionStatus.Failed => "bg-red-200",
        _ => ""
    };

    
}