using Microsoft.EntityFrameworkCore;
using TurboFrames;

namespace SolidGround;

public record InputTurboFrame(int InputId, int[] ExecutionIds, bool StartOpen) : TurboFrame(TurboFrameIdFor(InputId))
{
    public static string TurboFrameIdFor(int inputId) => $"input_{inputId}";

    protected override bool SkipTurboFrameTags => true;

    protected override Delegate RenderFunc => async (IServiceProvider serviceProvider, AppDbContext db) =>
    {
        var input = await db.Inputs.FindAsync(InputId) ?? throw new NotFoundException();

        var outputs = await db
            .Outputs
            .Where(o => o.InputId == InputId && ExecutionIds.Contains(o.ExecutionId))
            .ToListAsync();

        if (ExecutionIds.Contains(-1))
        {
            //-1 is a special meaning: it means that for each input just show the output it was first submitted with.
            var originalOutput = await db.Outputs.Where(o => o.InputId == InputId).OrderBy(o => o.Id).FirstOrDefaultAsync();
            if (originalOutput != null)
                outputs.Insert(0, originalOutput);
        }
        
        return new Html($"""
                    <turbo-frame id="{TurboFrameId}">
                        <details class="bg-white grow shadow-md rounded-lg group/output" {(StartOpen ? "open": "")}>
                           <summary class="p-4 cursor-pointer flex justify-between items-center rounded-lg">
                               {await new InputNameTurboFrame(InputId, EditMode:false).RenderAsync(serviceProvider)}
                               <svg class="w-5 h-5 transition-transform duration-200 group-open/output:rotate-90" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                                   <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"></path>
                               </svg>
                           </summary>
                           <div class="flex justify-between gap-2">
                            {await outputs.Select(output => new OutputTurboFrame(output.Id, true)).RenderAsync(serviceProvider)}
                            </div>
                           <details class="p-4">
                           <summary>Details about {input.Name}</summary>
                           {new InputDetailsTurboFrame(InputId).RenderLazy()}
                           </details>
                        </details>
                    </turbo-frame>
                    """);
    };
}