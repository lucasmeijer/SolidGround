using Microsoft.EntityFrameworkCore;
using TurboFrames;

namespace SolidGround;

record InputTurboFrame(int InputId, int[] ExecutionIds, bool StartOpen) : TurboFrame(TurboFrameIdFor(InputId))
{
    public static string TurboFrameIdFor(int inputId) => $"input_{inputId}";

    protected override bool SkipTurboFrameTags => true;

    protected override Delegate RenderFunc => async (AppDbContext db) =>
    {
        var input = await db.Inputs
            .AsNoTracking()
            .Where(i => i.Id == InputId)
            .Select(i => new { i.Id, i.Name, i.CreationTime })
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException();

        var inputItem = new InputListItem(input.Id, input.Name ?? TimeHelper.HowMuchTimeAgo(input.CreationTime));

        var outputs = await db.Outputs
            .AsNoTracking()
            .Where(o => o.InputId == InputId &&
                        (ExecutionIds.Contains(o.ExecutionId) ||
                         (ExecutionIds.Contains(-1) && !o.Execution.SolidGroundInitiated)))
            .OrderBy(o => o.ExecutionId)
            .ThenBy(o => o.Id)
            .Select(o => new OutputListItem(
                o.Id,
                o.InputId,
                o.Execution.Name ?? "Naamloos",
                o.Status,
                o.Cost))
            .ToArrayAsync();

        return RenderSummary(inputItem, outputs, StartOpen);
    };

    public static Html RenderSummary(InputListItem input, OutputListItem[] outputs, bool startOpen) => new($"""
                    <turbo-frame id="{TurboFrameIdFor(input.Id)}">
                        <details class="bg-white grow shadow-md rounded-lg group/output" {(startOpen ? "open" : "")}>
                           <summary class="p-4 cursor-pointer flex justify-between items-center rounded-lg">
                               {RenderInputName(input)}
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

    static Html RenderInputName(InputListItem input) => new($"""
                                                            <turbo-frame id="input_{input.Id}_name">
                                                                <h3 class="font-semibold">
                                                                    <a href="{InputEndPoints.Routes.api_input_id_name_edit.For(input.Id)}" data-turbo-frame="input_{input.Id}_name">
                                                                        {input.DisplayName}
                                                                    </a>
                                                                </h3>
                                                            </turbo-frame>
                                                            """);
}
