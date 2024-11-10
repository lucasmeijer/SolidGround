namespace SolidGround;

record ExecutionNameTurboFrame(int ExecutionId, bool EditMode) : EditableNameTurboFrame($"execution_{ExecutionId}_name", EditMode)
{
    protected override async Task<string> FindCurrentName(AppDbContext db)
    {
        var execution = await db.Executions.FindAsync(ExecutionId) ?? throw new BadHttpRequestException("ExecutionId not found");
        return execution.Name ?? HowMuchTimeAgo(execution.StartTime);
    }

    protected override string EditRoute => ExecutionsEndPoints.Routes.api_executions_id_name_edit.For(ExecutionId);
    protected override string ChangeNameEndPoint => ExecutionsEndPoints.Routes.api_executions_id_name.For(ExecutionId);
    
    static string HowMuchTimeAgo(DateTime dateTime)
    {
        var now = DateTime.Now;
        var difference = now - dateTime;

        if (difference.TotalSeconds < 60)
            return "Just now";

        if (difference.TotalMinutes < 60)
        {
            var minutes = (int)difference.TotalMinutes;
            return $"{minutes} minute{(minutes == 1 ? "" : "s")} ago";
        }

        if (difference.TotalHours < 24)
        {
            var hours = (int)difference.TotalHours;
            return $"{hours} hour{(hours == 1 ? "" : "s")} ago";
        }

        if (difference.TotalDays < 30)
        {
            var days = (int)difference.TotalDays;
            return $"{days} day{(days == 1 ? "" : "s")} ago";
        }

        if (difference.TotalDays < 365)
        {
            var months = (int)(difference.TotalDays / 30);
            return $"{months} month{(months == 1 ? "" : "s")} ago";
        }

        var years = (int)(difference.TotalDays / 365);
        return $"{years} year{(years == 1 ? "" : "s")} ago";
    }
}