using Microsoft.AspNetCore.Mvc;
using SolidGround.Pages;

namespace SolidGround;

static class UsageReportEndPoints
{
    public static class Routes
    {
        public static readonly RouteTemplate usage_report = RouteTemplate.Create("/usage-report");
        public static readonly string usage_report_month = "/usage-report/{month}";
    }

    public static void MapUsageReportEndPoints(this IEndpointRouteBuilder builder)
    {
        // Main usage report page
        builder.MapGet(Routes.usage_report, (Tenant tenant) =>
        {
            // Only allow access for assessment tenant
            if (tenant is not SchrijfEvenMeeAssessmentTenant)
                return (IResult)Results.NotFound();

            return new SolidGroundPage("Usage Report", new UsageReportPageBodyContent());
        });

        // Monthly detail page
        builder.MapGet(Routes.usage_report_month, async ([FromRoute] string month, AppDbContext db, Tenant tenant) =>
        {
            // Only allow access for assessment tenant
            if (tenant is not SchrijfEvenMeeAssessmentTenant)
                return (IResult)Results.NotFound();

            var data = await UsageReportService.GetMonthlyUsageDataAsync(db, month);

            if (data == null)
                return Results.NotFound();

            return new SolidGroundPage($"Usage Report - {month}", new MonthlyUsageDetailPageContent(data));
        });
    }
}
