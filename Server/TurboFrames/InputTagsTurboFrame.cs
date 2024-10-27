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
        
        return new Html($"""
                         <div class="flex-grow flex flex-wrap items-center gap-2">
                         {input.Tags.Render(RenderExistingTag)}
                         {RenderAvailableTags().Render()}
                         </div>
                         """);

        Html[] RenderAvailableTags() => AvailableTags().Length == 0
            ? []
            : [
                new($"""
                     <span class="inline-flex items-center px-6 h-12 rounded-full text-sm font-medium bg-blue-100 text-blue-800">
                     <div class="flex items-center space-x-2">
                         <form
                         data-controller="autosubmit formtojson"     
                         action="{InputEndPoints.Routes.api_input_id_tags.For(input.Id)}"    
                                                   
                         method="post" >
                             <select name="{JsonPropertyHelper.JsonNameFor((InputEndPoints.AddTagToInputDto o) => o.TagId)}" class="text-sm border-gray-300 rounded-md shadow-sm focus:border-indigo-300 focus:ring focus:ring-indigo-200 focus:ring-opacity-50">
                                 <option value="0">Select tag</option>
                                 {AvailableTags().Render(tag => new($"<option value=\"{tag.Id}\">{tag.Name}</option>"))}
                             </select>
                             <div data-formtojson-target="errorMessage" class="error-message"></div>
                         </form>
                     </div>
                     </span>
                     """)
            ];

        Html RenderExistingTag(Tag tag) => new($"""
                                                <span class="inline-flex items-center px-6 h-12 rounded-full text-sm font-medium bg-{tag.Color()}-100 text-{tag.Color()}-800">
                                                    {tag.Name}
                                                    <a href="{InputEndPoints.Routes.api_input_id_tags_tagid.For(input.Id, tag.Id)}" data-turbo-method="delete" data-turbo-confirm="Are you really really sure?" class="text-{tag.Color()}-400 hover:text-@color-600 focus:outline-none">
                                                      <svg class="h-4 w-4" fill="currentColor" viewBox="0 0 20 20">
                                                          <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd"/>
                                                      </svg>
                                                    </a>
                                                </span>
                                                """);

        Tag[] AvailableTags() => allTags.Where(at => input.Tags.All(it => it.Id != at.Id)).ToArray();
    };
}