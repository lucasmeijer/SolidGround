using System.Text.Json;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using TurboFrames;

namespace SolidGround.Pages;

record IndexPageBodyContent(AppState AppState) : PageFragment
{
    public override async Task<Html> RenderAsync(IServiceProvider serviceProvider)
    {
        var appDbContext = serviceProvider.GetRequiredService<AppDbContext>();

        var searchString = AppState.Search.Trim();

        int[] queryTags = AppState.Tags;
        var queryable = appDbContext.Inputs
            .Include(i => i.Tags)
            .Where(input => input.Tags.Count(t => queryTags.Contains(t.Id)) == queryTags.Length);
        
        if (!string.IsNullOrEmpty(searchString))
            queryable = queryable.Where(i => EF.Functions.Like(i.Name, $"%{searchString}%"));
        
        var inputIds = await queryable.Select(t => t.Id).ToArrayAsync();

        var tenant = serviceProvider.GetRequiredService<Tenant>();
        var appSnapShot = new AppSnapshot(AppState, inputIds);
        return new($"""
                    <script>
                        window.appSnapshot = JSON.parse(`{JsonSerializer.Serialize(appSnapShot, JsonSerializerOptions.Web)}`);
                    </script>
                    <div class="m-5 flex flex-col gap-4">
                        <div class="bg-white shadow-md rounded-lg group flex justify-center items-center p-4 text-lg">
                            SolidGround For {tenant.Identifier}
                        </div>
                       {await new FilterBarTurboFrame(AppState).RenderAsync(serviceProvider)}
                       {await new InputListTurboFrame(inputIds, AppState.Executions).RenderAsync(serviceProvider)}
                    </div>
                    """);
    }
}