using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using SolidGround.Pages;
using TurboFrames;

namespace SolidGround;

record InputDetailsTurboFrame(int InputId) : TurboFrame($"input_{InputId}_details")
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

        Html RenderStrings() => input.Strings.Count == 0 ? new Html() : new($"""
               <div class="border rounded p-4 bg-white">
                   <h3 class="font-semibold text-sm mb-3">Form Data</h3>
                   <div class="space-y-2">
                       {input.Strings.Render(str => new Html($"""
                           <div class="text-sm">
                               <div class="font-medium text-gray-700">{str.Name}</div>
                               <div class="text-gray-600 mt-1 whitespace-pre-wrap">{str.Value}</div>
                           </div>
                       """))}
                   </div>
               </div>
           """);

        Html RenderBody() => input.Files.Count > 0 || input.Strings.Count > 0 ? new Html() : new($"""
               <details class="p-2 border-b last:border-b-0 text-sm group/component">
                   <summary class="cursor-pointer flex justify-between items-center hover:bg-gray-50">
                       <h3 class="font-semibold">RequestBody</h3>
                       <svg class="w-5 h-5 transition-transform duration-200 transform rotate-0 group-open/component:rotate-90" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                           <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"></path>
                       </svg>
                   </summary>
                   <div class="py-2 text-xs">
                       {JsonFormatter.FormatMaybeJson(Encoding.UTF8.GetString(Convert.FromBase64String(input.OriginalRequest_Body)))}
                   </div>
               </details>
           """);
        
        return new Html($"""
                         <div class="p-4 flex-col flex gap-4">
                            <div class="flex flex-wrap gap-4">
                                 {input.Files.Render(inputFile => RenderInputFile(inputFile, input))}
                             </div>
                             {RenderStrings()}
                             {RenderBody()}    
                             <div class="flex justify-between">
                                 <div>
                                    {await new InputTagsTurboFrame(input.Id).RenderAsync(serviceProvider)}                  
                                 </div>
                                 <a href="{InputEndPoints.Routes.api_input_id.For(input.Id)}" data-turbo-method="delete" data-turbo-confirm="Delete Input?" class="{Buttons.Attrs} {Buttons.RedAttrs}">
                                     Delete Entire Input
                                 </a>
                             </div>
                             
                         </div>
                         """);
    };
//{await input.Outputs.Select(output => new OutputTurboFrame(output.Id, false)).RenderAsync(serviceProvider)}
    static Html RenderInputFile(InputFile inputFile, Input input)
    {
        var isImage = inputFile.MimeType.StartsWith("image/");
        if (isImage)
        {
            return new Html($"""
                <div class="border rounded overflow-hidden bg-gray-50">
                    <a href="{UrlFor(inputFile, input)}" target="_blank">
                        <img src="{UrlFor(inputFile, input)}" alt="{inputFile.Name}" class="max-w-sm max-h-96 object-contain">
                    </a>
                    <div class="p-2 text-xs text-gray-600">{inputFile.Name}</div>
                </div>
                """);
        }
        else
        {
            return new Html($"""
                <div class="border rounded p-4 bg-gray-50">
                    <a href="{UrlFor(inputFile, input)}" target="_blank" class="text-blue-600 hover:underline">
                        {inputFile.Name} ({inputFile.MimeType})
                    </a>
                </div>
                """);
        }
    }
    protected override string LazySrc => InputEndPoints.Routes.api_input_id_details.For(InputId);

    static string UrlFor(InputFile inputFile, Input input) => ImagesEndPoints.Routes.images_inputid_imageindex.For(input.Id, inputFile.Index, inputFile.Name);
}
