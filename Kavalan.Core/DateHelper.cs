namespace Kavalan.Core;

public static class DateHelper
{
    public static string CalculateLastSeen(this DateTime? lastSeenDateTime)
    {
        if (lastSeenDateTime == null) return "∞";

        return CalculateLastSeenInternal(lastSeenDateTime.Value);
    }
    public static string CalculateLastSeen(this DateTime lastSeenDateTime)
    {
        return CalculateLastSeenInternal(lastSeenDateTime);
    }

    private static string CalculateLastSeenInternal(DateTime dateTime)
    {
        TimeSpan delta = DateTime.Now - dateTime;

        if (delta.TotalDays >= 1)
            return $"{(int)delta.TotalDays} day(s) ago";

        if (delta.TotalHours >= 1)
            return $"{(int)delta.TotalHours} hour(s) ago";

        if (delta.TotalMinutes >= 1)
            return $"{(int)delta.TotalMinutes} minute(s) ago";

        return $"{delta.Seconds} second(s) ago";
    }
}
