using System.Text.Json.Nodes;
using TurboFrames;

namespace SolidGround.Pages;

public record SolidGroundPage(string Title, PageFragment BodyContent) : PageFragment
{
    static JsonObject ImportMap => new()
    {
        ["imports"] = new JsonObject
        {
            ["@hotwired/stimulus"] = "https://unpkg.com/@hotwired/stimulus@3.2.2/dist/stimulus.js",
            ["@hotwired/turbo"] = "https://unpkg.com/@hotwired/turbo@8.0.12/dist/turbo.es2017-esm.js"
        }
    };
    
    public sealed override async Task<Html> RenderAsync(IServiceProvider serviceProvider) => new($"""
         <!DOCTYPE html>
         <html lang="en">
         <head>
             <meta charset="utf-8"/>
             <meta name="viewport" content="width=device-width, initial-scale=1.0"/>
             <title>{Title}</title>
         
             <link rel="stylesheet" href="/lib/tailwindcss/tailwind.min.css">
             <link rel="stylesheet" href="/css/site.css">
         
             <script type="importmap">
             {ImportMap.ToJsonString()}
             </script>
         </head>
         <body id="body" style="scrollbar-gutter: stable; overflow-y: scroll" class="bg-gradient-to-br from-indigo-50 via-purple-50 to-pink-50">
         {await BodyContent.RenderAsync(serviceProvider)}
         <script src="/js/site.js" type="module"></script>
         </body>
         </html>
         """);
}