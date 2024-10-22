using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using TurboFrames;

namespace SolidGround;

// public record InputList(int[] InputIds) : TurboFrame("inputlist")
// {
//     public record Model(Input[] Inputs) : TurboFrameModel;
//
//     protected override async Task<TurboFrameModel> BuildModelAsync(IServiceProvider serviceProvider)
//     {
//         return new Model(await serviceProvider.GetRequiredService<AppDbContext>()
//             .Inputs.Where(i => InputIds.Contains(i.Id))
//             .ToArrayAsync());
//     }
// }

public record InputList2(int[] InputIds) : TurboFrame2("inputlist")
{
    protected override async Task<Html> RenderAsync(IServiceProvider serviceProvider) =>
        new($"""
              <div class="flex-col flex gap-2">
              {WarningElements().Render()}
              {await InputIds.Select(i => new InputTurboFrame2(i)).RenderAsync(serviceProvider)}
              </div>
             """);

    Html[] WarningElements() => InputIds.Length == 0
            ? [new("<div class=\"bg-white shadow-md rounded-lg p-4\">No inputs matching this filter.</div>")]
            : [];
}