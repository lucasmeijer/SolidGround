using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SolidGround.Pages;
using TurboFrames;

namespace SolidGround;

public record FilterBarTurboFrame(Tag[] Tags) : TurboFrame(TurboFrameId)
{
    public new static string TurboFrameId => "filter_bar";
    protected override Delegate RenderFunc => async (AppDbContext db, IServiceProvider sp) =>
    {
        var allTags = await db.Tags.ToArrayAsync();
        var availableTags = allTags.Where(at => Tags.All(t => t.Id != at.Id)).ToArray();
        var executions = await db.Executions.ToArrayAsync();
        return new Html($"""
                         <div data-controller="filterbar" data-filterbar-tags-value="{JsonSerializer.Serialize(Tags.Select(i => i.Id))}" 
                            class="bg-white shadow-md rounded-lg group flex flex-col p-4">
                            <div class="flex items-center gap-4 h-16">
                                <span class="text-gray-700 font-semibold">Search:</span>
                                <div class="relative flex-grow">
                                   <input data-turbo-permanent data-filterbar-target="searchbar" data-action="input->filterbar#sendFiltersToServer:debounce(300)" id="searchInput" type="text" placeholder="Enter your search" class="w-full border border-gray-300 rounded-md px-3 py-2 pr-10 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent">
                                   <button data-action="filterbar#clearSearchBar" class="absolute right-2 top-1/2 transform -translate-y-1/2 text-gray-400 hover:text-gray-600">
                                       <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
                                           <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" />
                                       </svg>
                                   </button>
                                 </div>
                                 <span class="text-gray-700 font-semibold">Filter:</span>
                               
                                 <div class="flex items-center gap-2 flex-none">
                                     {Tags.Render(RenderExistingTag)}
                                     <select data-action="change->filterbar#addTagDropdownChanged" class="text-sm border-gray-300 rounded-md shadow-sm focus:border-indigo-300 focus:ring focus:ring-indigo-200 focus:ring-opacity-50">
                                         <option value="">Add tag</option>
                                         {availableTags.Render(t => new($"""<option value="{t.Id}">{t.Name}</option>"""))}
                                     </select>
                                 </div>
                                 
                             </div>
                             <div class="grid grid-cols-4 gap-2">
                             {await executions.Select(e=>new ExecutionTurboFrame(e.Id)).RenderAsync(sp)}
                             </div>
                             </div>
                         """);
        /**/

        Html RenderExistingTag(Tag t) => new($"""
                                              <span class="inline-flex items-center px-6 h-12 rounded-full text-sm font-medium bg-{t.Color()}-100 text-{t.Color()}-800">
                                                {t.Name}
                                                <button data-action="click->filterbar#removeTag" data-filterbar-tagid-param="{t.Id}" class="text-{t.Color()}-400 hover:text-@color-600 focus:outline-none">
                                                    <svg class="h-4 w-4" fill="currentColor" viewBox="0 0 20 20">
                                                        <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd"/>
                                                    </svg>
                                                </button>
                                              </span>
                                              """);
    };
}

public record ExecutionTurboFrame(int ExecutionId) : TurboFrame(TurboFrameIdFor(ExecutionId))
{
    public static string TurboFrameIdFor(int executionId) => $"execution_{executionId}";
    protected override Delegate RenderFunc => async (IServiceProvider serviceProvider) => new Html($"""
          <div class="flex rounded-md bg-purple-100 items-center justify-between p-2">
             <div class="flex gap-2 items-center">
             <input type="checkbox" class="w-5 h-5 rounded-full text-purple-600 focus:ring-purple-500 cursor-pointer">
             {await new ExecutionNameTurboFrame(ExecutionId, EditMode: false).RenderAsync(serviceProvider)}
             </div>
             <a href="{ExecutionsEndPoints.Routes.api_executions_id.For(ExecutionId)}" data-turbo-method="delete" class="text-pink-500 hover:text-pink-700 transition-colors duration-200 p-2 rounded-full hover:bg-pink-50">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
              </svg>
             </a>
         </div>
         """);
}