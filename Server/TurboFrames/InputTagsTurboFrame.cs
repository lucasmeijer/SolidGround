using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using TurboFrames;

namespace SolidGround;

public record InputTagsTurboFrame(int InputId) : TurboFrame($"input_{InputId}_tags")
{
    protected override Delegate RenderFunc => async (AppDbContext db) =>
    {
        var input = await db
                        .Inputs
                        .Include(i => i.Tags)
                        .FirstOrDefaultAsync(i => i.Id == InputId)
                    ?? throw new BadHttpRequestException("input not found");

        var allTags = await db.Tags.ToArrayAsync();

        Html RenderExistingTag(Tag tag)
        {
            var color = tag.Color();

            var tagData = new JsonObject()
            {
                ["remove_tag"] = tag.Id,
                ["new_tags"] = NewTagsFor(input.Tags.Select(t => t.Id).Except([tag.Id]))
            }.ToJsonString();

            return new($"""
                        <span class="inline-flex items-center px-6 h-12 rounded-full text-sm font-medium bg-{color}-100 text-{color}-800">
                            {tag.Name}
                            <form method="post" action="{Endpoint}" class="inline-flex items-center ml-1.5">
                                <input type="hidden" name="tagData" value='{tagData}'/>
                                <button type="submit" class="text-{color}-400 hover:text-{color}-600 focus:outline-none">
                                  <svg class="h-4 w-4" fill="currentColor" viewBox="0 0 20 20">
                                      <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd"/>
                                  </svg>
                                </button>
                            </form>
                        </span>
                        """);
        }


        Html[] RenderAvailableTags()
        {
            var availableTags = allTags.Where(at => input.Tags.All(it => it.Id != at.Id)).ToArray();

            return availableTags.Length == 0
                ? []
                :
                [
                    new($"""
                         <span class="inline-flex items-center px-6 h-12 rounded-full text-sm font-medium bg-blue-100 text-blue-800">
                         <div class="flex items-center space-x-2">
                             <form method="post" action="{Endpoint}">
                                 <button type="submit" class="px-2 py-1 bg-indigo-100 text-indigo-700 rounded hover:bg-indigo-200 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-opacity-50 text-sm">
                                     Add:
                                 </button>
                                 <select name="tagData" class="text-sm border-gray-300 rounded-md shadow-sm focus:border-indigo-300 focus:ring focus:ring-indigo-200 focus:ring-opacity-50">
                                     <option value="">Select tag</option>
                                     {availableTags.Render(RenderAvailableTag)}
                                 </select>
                             </form>
                         </div>
                         </span>
                         """)
                ];
        }

        Html RenderAvailableTag(Tag tag)
        {
            var serialize = new JsonObject()
            {
                ["add_tag"] = tag.Id,
                ["new_tags"] = NewTagsFor(input.Tags.Select(t => t.Id).Append(tag.Id))
            }.ToJsonString();

            return new($"<option value='{serialize}'>{tag.Name}</option>");
        }

        return new Html($"""
                         <div class="flex-grow flex flex-wrap items-center gap-2">
                         {input.Tags.Render(RenderExistingTag)}
                         {RenderAvailableTags().Render()}
                         </div>
                         """);
    };

    string Endpoint => $"api/input/{InputId}/tags";

    static JsonNode? NewTagsFor(IEnumerable<int> tag_ids) => JsonSerializer.SerializeToNode(tag_ids);
}