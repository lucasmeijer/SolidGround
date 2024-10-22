using Microsoft.AspNetCore.Components;
using TurboFrames;

namespace SolidGround;

//
// [Route("/api/input/{InputId}")]
// public record InputTurboFrame(int InputId) : TurboFrame(TurboFrameIdFor(InputId))
// {
//     public record Model(Input Input) : TurboFrameModel;
//     
//     protected override Delegate BuildModelDelegate() => async (AppDbContext db) =>
//     {
//         return new Model(await db.Inputs.FindAsync(InputId) ?? throw new BadHttpRequestException("Input not found"));
//     };
//
//     protected override string[] AdditionalAttributes => ["data-turbo-permanent",..base.AdditionalAttributes];
//
//     public static string TurboFrameIdFor(int inputId) => $"input_{inputId}";
// }

[Route("/api/input/{InputId}")]
public record InputTurboFrame2(int InputId) : TurboFrame2(TurboFrameIdFor(InputId))
{
    public static string TurboFrameIdFor(int inputId) => $"input_{inputId}";

    protected override async Task<Html> RenderAsync(IServiceProvider serviceProvider) => new($"""
          <details class="bg-white grow shadow-md rounded-lg group/output">
             <summary class="p-4 cursor-pointer flex justify-between items-center rounded-lg">
                 {await new InputNameTurboFrame2(InputId).RenderIncludingTurboFrame(serviceProvider)}
                 <svg class="w-5 h-5 transition-transform duration-200 group-open/output:rotate-90" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                     <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"></path>
                 </svg>
             </summary>
              {new InputDetailsTurboFrame2(InputId).RenderLazy()}
             </details>
          """);
}