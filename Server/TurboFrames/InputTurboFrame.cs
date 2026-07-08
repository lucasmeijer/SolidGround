using Microsoft.EntityFrameworkCore;
using TurboFrames;

namespace SolidGround;

static class InputTurboFrame
{
    public static string TurboFrameIdFor(int inputId) => $"input_{inputId}";

    public static Html RenderSummary(InputListItem input, bool startOpen) => new($"""
                    <turbo-frame id="{TurboFrameIdFor(input.Id)}">
                        <details class="bg-white grow shadow-md rounded-lg group/output" {(startOpen ? "open" : "")}>
                           <summary class="p-4 cursor-pointer flex justify-between items-center rounded-lg">
                               <h3 class="font-semibold">{input.DisplayName}</h3>
                               <svg class="w-5 h-5 transition-transform duration-200 group-open/output:rotate-90" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                                   <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"></path>
                               </svg>
                           </summary>
                           {new InputContentTurboFrame(input.Id).RenderLazy()}
                        </details>
                    </turbo-frame>
                    """);
}

record InputContentTurboFrame(int InputId) : TurboFrame($"input_{InputId}_content")
{
    protected override string LazySrc => InputEndPoints.Routes.api_input_id_content.For(InputId);

    protected override Delegate RenderFunc => async (AppDbContext db, AppState appState) =>
    {
        var selectedExecutions = appState.Executions;
        var outputs = await db.Outputs
            .AsNoTracking()
            .Where(o => o.InputId == InputId &&
                        (selectedExecutions.Contains(o.ExecutionId) ||
                         (selectedExecutions.Contains(-1) && !o.Execution.SolidGroundInitiated)))
            .OrderBy(o => o.ExecutionId)
            .ThenBy(o => o.Id)
            .SelectListItems()
            .ToArrayAsync();

        return new Html($"""
                         <div class="p-4 flex flex-col gap-4">
                             {RenderOutputs(outputs)}
                             <details>
                                 <summary>Details</summary>
                                 {new InputDetailsTurboFrame(InputId).RenderLazy()}
                             </details>
                         </div>
                         """);
    };

    static Html RenderOutputs(OutputListItem[] outputs) => outputs.Length == 0
        ? new("""<div class="text-sm text-gray-500">No outputs for the selected executions.</div>""")
        : new($"""
              <div class="flex justify-between gap-2">
                  {outputs.Render(output => OutputTurboFrame.RenderSummary(output, false))}
              </div>
              """);
}
