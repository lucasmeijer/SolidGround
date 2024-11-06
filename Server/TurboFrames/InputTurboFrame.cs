using Microsoft.EntityFrameworkCore;
using TurboFrames;

namespace SolidGround;

public record InputTurboFrame(int InputId) : TurboFrame(TurboFrameIdFor(InputId))
{
    public static string TurboFrameIdFor(int inputId) => $"input_{inputId}";

    protected override bool SkipTurboFrameTags => true;

    protected override Delegate RenderFunc => async (IServiceProvider serviceProvider, AppDbContext db) =>
    {
        var input = await db.Inputs.Include(i=>i.Outputs).FirstOrDefaultAsync(i => i.Id == InputId);
        if (input == null)
            throw new NotFoundException();
                
        return new Html($"""
                    <turbo-frame id="{TurboFrameId}" data-turbo-permanent>
                        <details class="bg-white grow shadow-md rounded-lg group/output" open>
                           <summary class="p-4 cursor-pointer flex justify-between items-center rounded-lg">
                               {await new InputNameTurboFrame(InputId, EditMode:false).RenderAsync(serviceProvider)}
                               <svg class="w-5 h-5 transition-transform duration-200 group-open/output:rotate-90" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                                   <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"></path>
                               </svg>
                           </summary>
                           <div class="flex justify-between gap-2">
                            {await input.Outputs.OrderByDescending(o => o.Id).Take(2).Select(output => new OutputTurboFrame(output.Id, true)).RenderAsync(serviceProvider)}
                            </div>
                           <details class="p-4">
                           <summary>Details about input</summary>
                           {new InputDetailsTurboFrame(InputId).RenderLazy()}
                           </details>
                        </details>
                    </turbo-frame>
                    """);
    };
}