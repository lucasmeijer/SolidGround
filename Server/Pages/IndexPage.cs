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
        var queryable = appDbContext.Inputs.AsNoTracking();

        if (queryTags.Length > 0)
            queryable = queryable.Where(input => input.Tags.Count(t => queryTags.Contains(t.Id)) == queryTags.Length);
            
        if (!string.IsNullOrEmpty(searchString))
            queryable = queryable.Where(i =>
                EF.Functions.Like(i.Name, $"%{searchString}%") ||
                i.Outputs.Any(o =>
                    EF.Functions.Like(o.ClientAppIdentifier, $"%{searchString}%") ||
                    o.Components.Any(c => EF.Functions.Like(c.Value, $"%{searchString}%"))));

        var inputs = await queryable
            .OrderByDescending(i => i.CreationTime)
            .Take(100)
            .Select(i => new { i.Id, i.Name, i.CreationTime })
            .ToArrayAsync();

        var inputItems = inputs
            .Select(i => InputListItem.From(i.Id, i.Name, i.CreationTime))
            .ToArray();
        var inputIds = inputItems.Select(i => i.Id).ToArray();

        var selectedExecutions = AppState.Executions;
        OutputListItem[] outputItems = inputIds.Length == 0
            ? []
            : await appDbContext.Outputs
                .AsNoTracking()
                .Where(o => inputIds.Contains(o.InputId) &&
                            (selectedExecutions.Contains(o.ExecutionId) ||
                             (selectedExecutions.Contains(-1) && !o.Execution.SolidGroundInitiated)))
                .OrderBy(o => o.InputId)
                .ThenBy(o => o.ExecutionId)
                .ThenBy(o => o.Id)
                .SelectListItems()
                .ToArrayAsync();

        var tenant = serviceProvider.GetRequiredService<Tenant>();
        var freeDiskSpace = FreeDiskSpaceFor(serviceProvider.GetRequiredService<IConfiguration>());
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
                       {await new InputListTurboFrame(inputItems, outputItems).RenderAsync(serviceProvider)}
                       <div class="text-center text-xs text-gray-500 py-4">
                           Free disk space: {freeDiskSpace}
                       </div>
                    </div>
                    """);
    }

    static string FreeDiskSpaceFor(IConfiguration configuration)
    {
        try
        {
            var persistentStorage = Path.GetFullPath(configuration["PERSISTENT_STORAGE"] ?? ".");
            var root = Path.GetPathRoot(persistentStorage);
            return root == null ? "unknown" : FormatBytes(new DriveInfo(root).AvailableFreeSpace);
        }
        catch
        {
            return "unknown";
        }
    }

    static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value:0.#} {units[unit]}";
    }
}
