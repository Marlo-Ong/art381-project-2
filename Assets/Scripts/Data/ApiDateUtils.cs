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
}
