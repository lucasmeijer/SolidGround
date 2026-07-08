using TurboFrames;

namespace SolidGround;

static class InputTurboFrame
{
    public static string TurboFrameIdFor(int inputId) => $"input_{inputId}";

    public static Html RenderSummary(InputListItem input, OutputListItem[] outputs, bool startOpen) => new($"""
                    <turbo-frame id="{TurboFrameIdFor(input.Id)}">
                        <details class="bg-white grow shadow-md rounded-lg group/output" {(startOpen ? "open" : "")}>
                           <summary class="p-4 cursor-pointer flex justify-between items-center rounded-lg">
                               {EditableNameTurboFrame.ReadOnlyFrame(
                                   $"input_{input.Id}_name",
                                   input.DisplayName,
                                   InputEndPoints.Routes.api_input_id_name_edit.For(input.Id))}
                               <svg class="w-5 h-5 transition-transform duration-200 group-open/output:rotate-90" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                                   <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"></path>
                               </svg>
                           </summary>
                           <div class="flex justify-between gap-2">
                            {outputs.Render(output => OutputTurboFrame.RenderSummary(output, false))}
                            </div>
                           <details class="p-4">
                           <summary>Details about {input.DisplayName}</summary>
                           {new InputDetailsTurboFrame(input.Id).RenderLazy()}
                           </details>
                        </details>
                    </turbo-frame>
                    """);
}
