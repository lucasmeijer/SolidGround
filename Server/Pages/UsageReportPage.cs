using Microsoft.EntityFrameworkCore;
using TurboFrames;

namespace SolidGround.Pages;

record UsageReportPageBodyContent : PageFragment
{
    public override async Task<Html> RenderAsync(IServiceProvider serviceProvider)
    {
        var db = serviceProvider.GetRequiredService<AppDbContext>();
        var allMonths = await UsageReportService.GetMonthsWithCaseidsAsync(db);

        var monthButtons = string.Join("\n", allMonths.Select(month =>
            $"""
            <a href="/usage-report/{month}"
               class="block w-full text-left px-6 py-4 bg-white hover:bg-blue-50 border-b border-gray-200 transition-colors">
                <div class="flex justify-between items-center">
                    <span class="text-lg font-semibold text-gray-800">{month}</span>
                    <span class="text-gray-500 text-sm">Click to view details →</span>
                </div>
            </a>
            """));

        return new($"""
                    <div class="m-5 flex flex-col gap-4">
                        <div class="bg-white shadow-md rounded-lg p-6">
                            <h1 class="text-2xl font-bold text-gray-800 mb-2">Usage Report</h1>
                            <p class="text-gray-600">Assessment Tenant - Billable Cases by Month</p>
                        </div>

                        <div class="bg-white shadow-md rounded-lg overflow-hidden">
                            <div class="bg-gray-50 px-6 py-3 border-b border-gray-200">
                                <h2 class="text-sm font-semibold text-gray-700 uppercase tracking-wide">Select a Month</h2>
                            </div>
                            {(allMonths.Any()
                                ? monthButtons
                                : "<div class='px-6 py-8 text-center text-gray-500'>No months found</div>")}
                        </div>
                    </div>
                    """);
    }
}
