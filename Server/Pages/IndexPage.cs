using System.Text.Json;
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

        var inputIds = await queryable
            .OrderByDescending(i => i.CreationTime)
            .Take(100)
            .Select(t => t.Id).ToArrayAsync();

        var tenant = serviceProvider.GetRequiredService<Tenant>();
        var appSnapShot = new AppSnapshot(AppState, inputIds);

        // Show usage report link only for assessment tenant
        var usageReportLink = tenant is SchrijfEvenMeeAssessmentTenant
            ? """
              <div class="bg-white shadow-md rounded-lg p-4">
                  <a href="/usage-report" class="text-blue-600 hover:text-blue-800 font-medium">
                      📊 View Usage Report
                  </a>
              </div>
              """
            : "";

        return new($"""
                    <script>
                        window.appSnapshot = JSON.parse(`{JsonSerializer.Serialize(appSnapShot, JsonSerializerOptions.Web)}`);
                    </script>
                    <div class="m-5 flex flex-col gap-4">
                        <div class="bg-white shadow-md rounded-lg group flex justify-center items-center p-4 text-lg">
                            SolidGround For {tenant.Identifier}
                        </div>
                       {usageReportLink}
                       {await new FilterBarTurboFrame(AppState).RenderAsync(serviceProvider)}
                       {await new InputListTurboFrame(inputIds, AppState.Executions).RenderAsync(serviceProvider)}
                    </div>
                    """);
    }
}