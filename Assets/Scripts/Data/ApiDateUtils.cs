using System;
using System.Globalization;

public static class ApiDateUtils
{
    public static string GetCurrentUtcIsoString()
    {
        return DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
    }

    public static string GetExistingIsoOrNow(string isoString)
    {
        return string.IsNullOrWhiteSpace(isoString) ? GetCurrentUtcIsoString() : isoString;
    }

    public static string FormatIsoForDisplay(string isoString)
    {
        if (string.IsNullOrWhiteSpace(isoString))
            return "N/A";

        DateTime parsedValue;
        if (DateTime.TryParse(
            isoString,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out parsedValue))
            return parsedValue.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        return isoString;
    }

    public static string FormatIsoAsRelativeTime(string isoString)
    {
        if (string.IsNullOrWhiteSpace(isoString))
            return "unknown time";

        DateTime parsedValue;
        if (!DateTime.TryParse(
            isoString,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out parsedValue))
            return "unknown time";

        var elapsed = DateTime.UtcNow - parsedValue;
        if (elapsed.TotalSeconds < 1d)
            return "just now";

        if (elapsed.TotalSeconds < 60d)
            return FormatRelativeUnit((int)Math.Floor(elapsed.TotalSeconds), "second");

        if (elapsed.TotalMinutes < 60d)
            return FormatRelativeUnit((int)Math.Floor(elapsed.TotalMinutes), "minute");

        if (elapsed.TotalHours < 24d)
            return FormatRelativeUnit((int)Math.Floor(elapsed.TotalHours), "hour");

        if (elapsed.TotalDays < 7d)
            return FormatRelativeUnit((int)Math.Floor(elapsed.TotalDays), "day");

        if (elapsed.TotalDays < 30d)
            return FormatRelativeUnit((int)Math.Floor(elapsed.TotalDays / 7d), "week");

        if (elapsed.TotalDays < 365d)
            return FormatRelativeUnit((int)Math.Floor(elapsed.TotalDays / 30d), "month");

        return FormatRelativeUnit((int)Math.Floor(elapsed.TotalDays / 365d), "year");
    }

    private static string FormatRelativeUnit(int value, string unit)
    {
        var clampedValue = Math.Max(1, value);
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0} {1}{2} ago",
            clampedValue,
            unit,
            clampedValue == 1 ? string.Empty : "s");
    }
}
