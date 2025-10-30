using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace SolidGround;

record MonthlyUsageData(
    string Month,
    int TotalInputs,
    int InputsWithCaseidField,
    int UniqueNonEmptyCaseids,
    int EmptyCaseidCount,
    int InputsWithoutCaseidField,
    Dictionary<string, int> CaseidAttemptCounts,
    List<string> CaseidList,
    Dictionary<int, int> UniqueCasesByDay
);

static class UsageReportService
{
    public static async Task<List<string>> GetMonthsWithCaseidsAsync(AppDbContext db)
    {
        // Get distinct months that have any inputs (lightweight query)
        var months = await db.Inputs
            .Select(i => new { i.CreationTime.Year, i.CreationTime.Month })
            .Distinct()
            .OrderByDescending(m => m.Year)
            .ThenByDescending(m => m.Month)
            .ToListAsync();

        return months.Select(m => $"{m.Year}-{m.Month:D2}").ToList();
    }

    public static async Task<MonthlyUsageData?> GetMonthlyUsageDataAsync(AppDbContext db, string month)
    {
        // Parse month string (e.g., "2025-10")
        var parts = month.Split('-');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var year) || !int.TryParse(parts[1], out var monthNum))
            return null;

        // Only query inputs for this specific month
        var startDate = new DateTime(year, monthNum, 1);
        var endDate = startDate.AddMonths(1);

        var inputs = await db.Inputs
            .Where(i => i.CreationTime >= startDate && i.CreationTime < endDate)
            .Select(i => new { i.Id, i.CreationTime, i.OriginalRequest_Body })
            .ToListAsync();

        return CalculateMonthlyStats(month, inputs, year, monthNum);
    }

    private static MonthlyUsageData CalculateMonthlyStats(string month, IEnumerable<dynamic> inputs, int year, int monthNum)
    {
        var inputList = inputs.ToList();
        int totalInputs = inputList.Count;
        int inputsWithCaseidField = 0;
        int emptyCaseidCount = 0;
        int inputsWithoutCaseidField = 0;
        var uniqueCaseids = new HashSet<string>();
        var caseidAttemptCounts = new Dictionary<string, int>();
        var casesByDay = new Dictionary<int, HashSet<string>>();

        // Initialize all days in the month to empty sets
        var daysInMonth = DateTime.DaysInMonth(year, monthNum);
        for (int day = 1; day <= daysInMonth; day++)
        {
            casesByDay[day] = new HashSet<string>();
        }

        foreach (var input in inputList)
        {
            try
            {
                var jsonBytes = Convert.FromBase64String((string)input.OriginalRequest_Body);
                var jsonString = Encoding.UTF8.GetString(jsonBytes);
                var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);

                if (data != null && data.TryGetValue("caseid", out var caseidElement))
                {
                    inputsWithCaseidField++;

                    var caseid = caseidElement.GetString() ?? "";

                    if (!string.IsNullOrWhiteSpace(caseid))
                    {
                        uniqueCaseids.Add(caseid);
                        if (!caseidAttemptCounts.ContainsKey(caseid))
                            caseidAttemptCounts[caseid] = 0;
                        caseidAttemptCounts[caseid]++;

                        // Track unique cases by day
                        DateTime creationTime = input.CreationTime;
                        casesByDay[creationTime.Day].Add(caseid);
                    }
                    else
                    {
                        emptyCaseidCount++;
                    }
                }
                else
                {
                    inputsWithoutCaseidField++;
                }
            }
            catch
            {
                inputsWithoutCaseidField++;
            }
        }

        // Convert to counts
        var uniqueCasesByDay = casesByDay.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count);

        return new MonthlyUsageData(
            Month: month,
            TotalInputs: totalInputs,
            InputsWithCaseidField: inputsWithCaseidField,
            UniqueNonEmptyCaseids: uniqueCaseids.Count,
            EmptyCaseidCount: emptyCaseidCount,
            InputsWithoutCaseidField: inputsWithoutCaseidField,
            CaseidAttemptCounts: caseidAttemptCounts,
            CaseidList: uniqueCaseids.OrderBy(c => c).ToList(),
            UniqueCasesByDay: uniqueCasesByDay
        );
    }
}
