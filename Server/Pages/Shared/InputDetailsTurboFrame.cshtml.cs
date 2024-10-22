using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using SolidGround.Pages;
using TurboFrames;

namespace SolidGround;

// [Route("/api/input/{InputId}/details")]
// public record InputDetailsTurboFrame(int InputId) : TurboFrame($"input_{InputId}_details")
// {
//     public record Model(Input Input) : TurboFrameModel;
//
//     public LazyFrame Lazy => new(TurboFrameId, $"/api/input/{InputId}/details");
//
//     protected override async Task<TurboFrameModel> BuildModelAsync(IServiceProvider serviceProvider)
//     {
//         var dbContext = serviceProvider.GetRequiredService<AppDbContext>();
//         var input = await dbContext.Inputs
//             .Include(i => i.Tags)
//             .Include(i => i.Outputs)
//             .Include(i => i.Files)
//             .Include(i => i.Strings)
//             .AsSplitQuery()
//             .FirstOrDefaultAsync(i => i.Id == InputId) ?? throw new BadHttpRequestException("input not found");
//
//         return new Model(input);
//     }
// }

[Route("/api/input/{InputId}/details")]
public record InputDetailsTurboFrame2(int InputId) : TurboFrame2($"input_{InputId}_details")
{
    protected override async Task<Html> RenderAsync(IServiceProvider serviceProvider)
    {
        var input = await serviceProvider.GetRequiredService<AppDbContext>().Inputs
            .Include(i => i.Tags)
            .Include(i => i.Outputs)
            .Include(i => i.Files)
            .Include(i => i.Strings)
            .AsSplitQuery()
            .FirstOrDefaultAsync(i => i.Id == InputId) ?? throw new BadHttpRequestException("input not found");
        return await Render(input, serviceProvider);
    }
    
    protected override string LazySrc => $"/api/input/{InputId}/details";
    
    static async Task<Html> Render(Input input, IServiceProvider serviceProvider) => new($"""
         <div class="p-4 flex-col flex gap-4">
             <div class="flex gap-2">
                 {input.Files.Render(inputFile => RenderInputFile(inputFile, input))}
             </div>
                 
             <div class="flex justify-between">
                 <div>
                    {await new InputTagsTurboFrame(input.Id).RenderIncludingTurboFrame(serviceProvider)}                  
                 </div>
                 <a href="/api/input/{input.Id}" data-turbo-method="delete" class="{Buttons.Attrs} {Buttons.RedAttrs}">
                     Delete Entire Input
                 </a>
             </div>
             {await input.Outputs.Select(output => new OutputTurboFrame2(output.Id)).RenderAsync(serviceProvider)}
         </div>
         """);

    static Html RenderInputFile(InputFile inputFile, Input input) => new($"""
                                                                          <div class="w-32 h-32 overflow-hidden">
                                                                              <a href="/images/{input.Id}/{inputFile.Index}" target="_blank">
                                                                                  <img src="/images/{input.Id}/{inputFile.Index}" alt="Your image description" class="w-full h-full object-cover">
                                                                              </a>
                                                                          </div>
                                                                          """);
}
