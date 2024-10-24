using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using SolidGround.Pages;
using TurboFrames;

namespace SolidGround;

public record InputDetailsTurboFrame(int InputId) : TurboFrame($"input_{InputId}_details")
{
    protected override async Task<Html> RenderContentsAsync(IServiceProvider serviceProvider)
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

    protected override string LazySrc => InputEndPoints.Routes.api_input_id_details.For(InputId);
    
    static async Task<Html> Render(Input input, IServiceProvider serviceProvider) => new($"""
         <div class="p-4 flex-col flex gap-4">
             <div class="flex gap-2">
                 {input.Files.Render(inputFile => RenderInputFile(inputFile, input))}
             </div>
                 
             <div class="flex justify-between">
                 <div>
                    {await new InputTagsTurboFrame(input.Id).RenderAsync(serviceProvider)}                  
                 </div>
                 <a href="{InputEndPoints.Routes.api_input_id.For(input.Id)}" data-turbo-method="delete" class="{Buttons.Attrs} {Buttons.RedAttrs}">
                     Delete Entire Input
                 </a>
             </div>
             {await input.Outputs.Select(output => new OutputTurboFrame(output.Id)).RenderAsync(serviceProvider)}
         </div>
         """);

    static Html RenderInputFile(InputFile inputFile, Input input)
    {
        var url = ImagesEndPoints.Routes.images_inputid_imageindex.For(input.Id, inputFile.Index);
        return new($"""
                    <div class="w-32 h-32 overflow-hidden">
                        <a href="{url}" target="_blank">
                            <img src="{url}" alt="Your image description" class="w-full h-full object-cover">
                        </a>
                    </div>
                    """);
    }
}
