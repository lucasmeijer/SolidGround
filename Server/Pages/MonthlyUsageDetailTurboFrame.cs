using TurboFrames;

namespace SolidGround.Pages;

record MonthlyUsageDetailPageContent(MonthlyUsageData Data) : PageFragment
{
    public override Task<Html> RenderAsync(IServiceProvider serviceProvider)
    {
        // Calculate histogram of attempts
        var attemptHistogram = Data.CaseidAttemptCounts.Values
            .GroupBy(attempts => attempts)
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.Count());

        var maxHistogramCount = attemptHistogram.Values.Max();
        var histogramBars = string.Join("\n", attemptHistogram.Select(kvp =>
        {
            var attempts = kvp.Key;
            var count = kvp.Value;
            var percentage = (count * 100.0 / maxHistogramCount);
            var barWidth = Math.Max(percentage, 5); // Minimum 5% for visibility

            return $"""
                <div class="flex items-center gap-3">
                    <div class="w-24 text-right text-sm font-medium text-gray-700">{attempts} attempt{(attempts != 1 ? "s" : "")}</div>
                    <div class="bg-gray-200 rounded-full h-8 relative overflow-hidden" style="width: 600px; max-width: 100%;">
                        <div class="bg-blue-500 h-full rounded-full flex items-center justify-end px-3" style="width: {barWidth:F1}%;">
                            <span class="text-white text-sm font-semibold">{count}</span>
                        </div>
                    </div>
                </div>
                """;
        }));

        var caseidListHtml = string.Join("\n", Data.CaseidList.Select((caseid, index) =>
        {
            var attempts = Data.CaseidAttemptCounts[caseid];
            return $"""
                <tr class="{(index % 2 == 0 ? "bg-gray-50" : "bg-white")}">
                    <td class="px-4 py-2 text-sm text-gray-800 font-mono">{caseid}</td>
                    <td class="px-4 py-2 text-sm text-gray-600 text-center">{attempts}</td>
                </tr>
                """;
        }));

        // Parse month to get year and month number for day-of-week calculation
        var monthParts = Data.Month.Split('-');
        var year = int.Parse(monthParts[0]);
        var monthNum = int.Parse(monthParts[1]);

        // Daily unique cases graph
        var maxCases = Data.UniqueCasesByDay.Values.Max();
        if (maxCases == 0) maxCases = 1; // Avoid division by zero

        var dailyBars = string.Join("\n", Data.UniqueCasesByDay.OrderBy(kvp => kvp.Key).Select(kvp =>
        {
            var day = kvp.Key;
            var cases = kvp.Value;
            var date = new DateTime(year, monthNum, day);
            var dayOfWeek = date.DayOfWeek;
            var isWeekend = dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday;
            var heightPx = cases > 0 ? (cases * 200.0 / maxCases) : 5; // 5px minimum for zero values
            var barColor = isWeekend ? "bg-blue-400" : "bg-green-500";
            var bgColor = isWeekend ? "bg-blue-50" : "bg-white";

            return $"""
                <div class="flex flex-col items-center gap-1 px-1 py-2 {bgColor} rounded" style="min-width: 32px;">
                    <div class="w-full flex items-end justify-center" style="height: 200px;">
                        <div class="w-full {barColor} rounded-t transition-all hover:opacity-80" style="height: {heightPx:F1}px; min-height: 2px;" title="{date:ddd, MMM d}: {cases} unique case{(cases != 1 ? "s" : "")}"></div>
                    </div>
                    <div class="text-xs text-gray-600 font-medium">{day}</div>
                    <div class="text-xs text-gray-500 font-bold">{cases}</div>
                </div>
                """;
        }));

        return Task.FromResult(new Html($"""
            <div class="m-5 flex flex-col gap-4">
                <div class="bg-white shadow-md rounded-lg overflow-hidden">
                    <!-- Header -->
                    <div class="bg-blue-600 px-6 py-4">
                        <div class="flex items-center justify-between">
                            <div>
                                <h2 class="text-2xl font-bold text-white">{Data.Month}</h2>
                                <p class="text-blue-100 text-sm mt-1">Detailed Usage Statistics</p>
                            </div>
                            <a href="/usage-report" class="text-white hover:text-blue-100 underline">← Back to all months</a>
                        </div>
                    </div>

                    <!-- Summary Stats -->
                    <div class="grid grid-cols-2 md:grid-cols-4 gap-4 p-6 bg-gray-50 border-b border-gray-200">
                        <div class="text-center">
                            <div class="text-3xl font-bold text-blue-600">{Data.UniqueNonEmptyCaseids}</div>
                            <div class="text-sm text-gray-600 mt-1">Billable Cases</div>
                        </div>
                        <div class="text-center">
                            <div class="text-3xl font-bold text-gray-800">{Data.CaseidAttemptCounts.Values.Sum()}</div>
                            <div class="text-sm text-gray-600 mt-1">Total Attempts</div>
                        </div>
                        <div class="text-center">
                            <div class="text-3xl font-bold text-gray-600">{Data.InputsWithoutCaseidField}</div>
                            <div class="text-sm text-gray-600 mt-1">No caseid field</div>
                        </div>
                        <div class="text-center">
                            <div class="text-3xl font-bold text-gray-400">{Data.EmptyCaseidCount}</div>
                            <div class="text-sm text-gray-600 mt-1">Empty caseid</div>
                        </div>
                    </div>

                    <!-- Daily Unique Cases Graph -->
                    <div class="p-6 border-b border-gray-200">
                        <div class="flex items-center justify-between mb-4">
                            <h3 class="text-lg font-semibold text-gray-800">Unique Cases by Day of Month</h3>
                            <div class="flex items-center gap-4 text-sm">
                                <div class="flex items-center gap-2">
                                    <div class="w-4 h-4 bg-green-500 rounded"></div>
                                    <span class="text-gray-600">Weekday</span>
                                </div>
                                <div class="flex items-center gap-2">
                                    <div class="w-4 h-4 bg-blue-400 rounded"></div>
                                    <span class="text-gray-600">Weekend</span>
                                </div>
                            </div>
                        </div>
                        <div class="flex items-end gap-0.5 bg-gray-100 p-4 rounded-lg overflow-x-auto">
                            {dailyBars}
                        </div>
                    </div>

                    <!-- Attempt Histogram -->
                    <div class="p-6 border-b border-gray-200">
                        <h3 class="text-lg font-semibold text-gray-800 mb-4">Attempt Distribution per Case</h3>
                        <div class="space-y-3">
                            {histogramBars}
                        </div>
                        <p class="text-sm text-gray-500 mt-4 italic">
                            Average: {(Data.CaseidAttemptCounts.Values.Sum() / (double)Data.UniqueNonEmptyCaseids):F1} attempts per case
                        </p>
                    </div>

                    <!-- Caseid List (Collapsible) -->
                    <div class="p-6">
                        <details class="group">
                            <summary class="cursor-pointer list-none">
                                <h3 class="text-lg font-semibold text-gray-800 mb-4 inline-flex items-center gap-2">
                                    <span class="group-open:rotate-90 transition-transform">▶</span>
                                    Complete List of Case IDs ({Data.UniqueNonEmptyCaseids})
                                    <span class="text-sm font-normal text-gray-500">(click to expand)</span>
                                </h3>
                            </summary>
                            <div class="mt-4 overflow-x-auto border border-gray-200 rounded-lg">
                                <table class="w-full">
                                    <thead class="bg-gray-100 border-b border-gray-200">
                                        <tr>
                                            <th class="px-4 py-3 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">Case ID</th>
                                            <th class="px-4 py-3 text-center text-xs font-semibold text-gray-700 uppercase tracking-wider">Attempts</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {caseidListHtml}
                                    </tbody>
                                </table>
                            </div>
                        </details>
                    </div>

                    <!-- Footer Note -->
                    <div class="px-6 py-4 bg-gray-50 text-sm text-gray-600">
                        <p><strong>Note:</strong> Only non-empty case IDs are counted as billable. Cases with empty or missing case IDs are not charged.</p>
                    </div>
                </div>
            </div>
            """));
    }
}
