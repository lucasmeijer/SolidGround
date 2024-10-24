using Microsoft.EntityFrameworkCore;
using TurboFrames;

namespace SolidGround.Pages;

public record TagsPage() : SolidGroundPage("Tags")
{
    protected override async Task<Html> RenderBodyContent(IServiceProvider serviceProvider)
    {
        var db = serviceProvider.GetRequiredService<AppDbContext>();
        var tags = await db.Tags.ToArrayAsync();
        var withCounts = await Task.WhenAll(tags.Select(t => db.Inputs.CountAsync(i => i.Tags.Contains(t))));
        
        Html RenderExistingTag(Tag t) => new($"""
                  <span class="inline-flex items-center px-6 h-12 rounded-full text-sm font-medium bg-{t.Color()}-100 text-{t.Color()}-800">
                    {t.Name} ({withCounts[Array.IndexOf(tags,t)]})
                    <a href="{TagEndPoints.Routes.api_tags_id_delete.For(t.Id)}" data-turbo-confirm="Are you really really sure?" class="text-{t.Color()}-400 hover:text-@color-600 focus:outline-none">
                        <svg class="h-4 w-4" fill="currentColor" viewBox="0 0 20 20">
                            <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd"/>
                        </svg>
                    </a>
                  </span>
                  """);

        return new($"""
                    <div class="w-1/2 mx-auto p-4 my-10 bg-gray-100 rounded-lg shadow-md flex flex-col items-center gap-2">
                      <h2 class="text-2xl font-bold mb-4">Tags management</h2>
                        {tags.Render(RenderExistingTag)}  
                    
                        <form
                        data-turbo-frame="_top" action="{TagEndPoints.Routes.api_tags}" 
                        method="post"
                        class="flex items-center space-x-4 max-w-md mx-auto p-4"
                        >
                      <input
                        name="tag_name",
                        type="text"
                        placeholder="Type new tag name"
                        class="flex-grow px-4 py-2 text-gray-700 bg-white border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                      />
                      <button
                        type="submit"
                        class="px-6 py-2 text-white bg-blue-600 rounded-lg hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-opacity-50 transition-colors duration-300"
                      >
                        Add as new tag
                      </button>
                    </form>
                    </div>
                    """);
    }
}