using Microsoft.EntityFrameworkCore;

namespace SolidGround.Pages;

public record IndexPage() : SolidGroundPage("SolidGround")
{
    protected override async Task<Html> RenderBodyContent(IServiceProvider serviceProvider)
    {
        var appDbContext = serviceProvider.GetRequiredService<AppDbContext>();
        
        var appState = serviceProvider.GetRequiredService<AppState>();
        
        var searchString = appState.Search.Trim();
        
        var queryable = appDbContext.Inputs
            .Include(i => i.Tags)
            .Where(i => appState.Tags.All(searchTagId => i.Tags.Any(it => it.Id == searchTagId)));
        
        if (!string.IsNullOrEmpty(searchString))
            queryable = queryable.Where(i => EF.Functions.Like(i.Name, $"%{searchString}%"));
        
        var inputIds = await queryable.Select(t => t.Id).ToArrayAsync();
        
        return new($"""
                    <div class="m-5 flex flex-col gap-4">
                       {await new FilterBarTurboFrame(appState).RenderAsync(serviceProvider)}
                       {await new InputListTurboFrame(inputIds, appState.Executions).RenderAsync(serviceProvider)}
                    </div>
                    """);
    }
}