using TurboFrames;

namespace SolidGround;

public record InputTurboFrame(int InputId) : TurboFrame(TurboFrameIdFor(InputId))
{
    public static string TurboFrameIdFor(int inputId) => $"input_{inputId}";

    protected override bool SkipTurboFrameTags => true;

    protected override Delegate RenderFunc => async (IServiceProvider serviceProvider, AppDbContext db) =>
    {
        var input = await db.Inputs.FindAsync(InputId);
        if (input == null)
            throw new NotFoundException();
                
        return new Html($"""
                    <turbo-frame id="{TurboFrameId}" data-turbo-permanent>
                        <details class="bg-white grow shadow-md rounded-lg group/output">
                           <summary class="p-4 cursor-pointer flex justify-between items-center rounded-lg">
                               {await new InputNameTurboFrame(InputId).RenderAsync(serviceProvider)}
                               <svg class="w-5 h-5 transition-transform duration-200 group-open/output:rotate-90" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                                   <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"></path>
                               </svg>
                           </summary>
                           {new InputDetailsTurboFrame(InputId).RenderLazy()}
                        </details>
                    </turbo-frame>
                    """);
    };
}