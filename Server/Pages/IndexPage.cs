using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using TurboFrames;

namespace SolidGround.Pages;

public record IndexPage : PageFragment
{
    public override async Task<Html> RenderAsync(IServiceProvider serviceProvider) => new($"""
         <!DOCTYPE html>
         <html lang="en">
         <head>
             <meta charset="utf-8"/>
             <meta name="viewport" content="width=device-width, initial-scale=1.0"/>
             <title>SolidGround</title>
         
             <link rel="stylesheet" href="lib/tailwindcss/tailwind.min.css">
             <link rel="stylesheet" href="css/site.css">
         
             <script type="importmap">
             {Importmap.ToJsonString()}
             </script>
         </head>
         <body style="scrollbar-gutter: stable; overflow-y: scroll">
         
          <div class="m-5 flex flex-col gap-4">
             <details class="bg-white shadow-md rounded-lg group">
                 <summary class="p-4 cursor-pointer flex justify-between items-center bg-green-100 rounded-lg">
                     <h3 class="font-semibold">Run Experiment</h3>
                     <svg class="w-5 h-5 transition-transform duration-200 group-open:rotate-90" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                         <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"></path>
                     </svg>
                 </summary>
                 
                 <div class="p-4">
                     {new RunExperimentTurboFrame().RenderLazy()}
                 </div>
             </details>
             
             {await new FilterBarTurboFrame([]).RenderAsync(serviceProvider)}
             {await new InputListTurboFrame(await AllInputsIdsFrom(serviceProvider)).RenderAsync(serviceProvider)}
         </div>
         <script src="js/site.js" type="module"></script>
         </body>
         </html>
         """);

    static async Task<int[]> AllInputsIdsFrom(IServiceProvider serviceProvider) =>
        await serviceProvider.GetRequiredService<AppDbContext>()
            .Inputs
            .Select(i => i.Id)
            .ToArrayAsync();

    static JsonObject Importmap => new()
    {
        ["imports"] = new JsonObject
        {
            ["@hotwired/stimulus"] = "https://unpkg.com/@hotwired/stimulus@3.2.2/dist/stimulus.js",
            ["@hotwired/turbo"] = "https://unpkg.com/@hotwired/turbo@8.0.12/dist/turbo.es2017-esm.js"
        }
    };
}