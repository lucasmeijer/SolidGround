using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TurboFrames;

namespace SolidGround.Pages;

public record IndexPageBodyContent(AppState AppState) : PageFragment
{
    public override async Task<Html> RenderAsync(IServiceProvider serviceProvider)
    {
        var appDbContext = serviceProvider.GetRequiredService<AppDbContext>();

        var searchString = AppState.Search.Trim();
        
        var queryable = appDbContext.Inputs
            .Include(i => i.Tags)
            .Where(i => AppState.Tags.All(searchTagId => i.Tags.Any(it => it.Id == searchTagId)));
        
        if (!string.IsNullOrEmpty(searchString))
            queryable = queryable.Where(i => EF.Functions.Like(i.Name, $"%{searchString}%"));
        
        var inputIds = await queryable.Select(t => t.Id).ToArrayAsync();

        var appSnapShot = new AppSnapshot(AppState, inputIds);
        return new($"""
                    <script>
                        window.appSnapshot = JSON.parse(`{JsonSerializer.Serialize(appSnapShot, JsonSerializerOptions.Web)}`);
                    </script>
                    <div class="m-5 flex flex-col gap-4">
                       {await new FilterBarTurboFrame(AppState).RenderAsync(serviceProvider)}
                       {await new InputListTurboFrame(inputIds, AppState.Executions).RenderAsync(serviceProvider)}
                    </div>
                    """);
    }
}