namespace SolidGround;

static class TimeHelper
{
    public static string HowMuchTimeAgo(DateTime dateTime)
    {
        var now = DateTime.UtcNow;
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