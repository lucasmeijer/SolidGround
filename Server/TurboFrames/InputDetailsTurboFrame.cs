using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using SolidGround.Pages;
using TurboFrames;

namespace SolidGround;

public record InputDetailsTurboFrame(int InputId) : TurboFrame($"input_{InputId}_details")
{
    protected override Delegate RenderFunc => async (IServiceProvider serviceProvider, AppDbContext db) =>
    {
        var input = await db.Inputs
            .Include(i => i.Tags)
            .Include(i => i.Outputs)
            .Include(i => i.Files)
            .Include(i => i.Strings)
            .AsSplitQuery()
            .FirstOrDefaultAsync(i => i.Id == InputId) ?? throw new BadHttpRequestException("input not found");
        return new Html($"""
                         <div class="p-4 flex-col flex gap-4">
                            <div class="flex justify-between gap-2">
                            {await input.Outputs.OrderByDescending(o => o.Id).Take(2).Select(output => new OutputTurboFrame(output.Id, true)).RenderAsync(serviceProvider)}
                            </div>
                         
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
                             
                         </div>
                         """);
    };
//{await input.Outputs.Select(output => new OutputTurboFrame(output.Id, false)).RenderAsync(serviceProvider)}
    static Html RenderInputFile(InputFile inputFile, Input input) => new($"""
                                                                          <div class="w-32 h-32 overflow-hidden">
                                                                              <a href="{UrlFor(inputFile, input)}" target="_blank">
                                                                                  <img src="{UrlFor(inputFile, input)}" alt="Your image description" class="w-full h-full object-cover">
                                                                              </a>
                                                                          </div>
                                                                          """);

    protected override string LazySrc => InputEndPoints.Routes.api_input_id_details.For(InputId);

    static string UrlFor(InputFile inputFile, Input input) => ImagesEndPoints.Routes.images_inputid_imageindex.For(input.Id, inputFile.Index);
}
