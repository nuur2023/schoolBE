using schoolBE.Models;

namespace schoolBE.Services;

public static class AutoChargeScheduleHelper
{
    /// <summary>
    /// Parses times saved from the UI as "HH:mm" or "H:mm" (24h). 
    /// .NET's TimeSpan.TryParse does not accept "12:25" (needs "12:25:00"), which caused scheduled times to be ignored.
    /// </summary>
    public static bool TryParseTimeOfDay(string? s, out TimeSpan timeOfDay)
    {
        timeOfDay = default;
        if (string.IsNullOrWhiteSpace(s))
            return false;

        var t = s.Trim();
        // Allow optional seconds: "12:25:00"
        var parts = t.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
            return false;

        if (!int.TryParse(parts[0], out var h) || !int.TryParse(parts[1], out var m))
            return false;

        var sec = 0;
        if (parts.Length >= 3 && int.TryParse(parts[2], out var sParsed))
            sec = sParsed;

        if (h is < 0 or > 23 || m is < 0 or > 59 || sec is < 0 or > 59)
            return false;

        timeOfDay = new TimeSpan(h, m, sec);
        return true;
    }

    /// <param name="timeMatchWindowMinutes">Half-window around scheduled time for background polling (e.g. 15 with 2-minute ticks).</param>
    public static bool ShouldRun(AutoChargeSetting s, DateTime nowLocal, int timeMatchWindowMinutes = 15)
    {
        if (!s.IsActive || !s.IsAllowedAutoCharge)
            return false;

        if (nowLocal.Day != s.DayOfMonth)
            return false;

        if (string.Equals(s.MonthsMode, "specific", StringComparison.OrdinalIgnoreCase))
        {
            var months = ParseMonths(s.SpecificMonths);
            if (months.Count == 0 || !months.Contains(nowLocal.Month))
                return false;
        }

        if (s.LastRunOn.HasValue && s.LastRunOn.Value.Date == nowLocal.Date)
            return false;

        if (!string.IsNullOrWhiteSpace(s.ChargeTimeLocal))
        {
            if (!TryParseTimeOfDay(s.ChargeTimeLocal, out var target))
                return false;

            var diffMinutes = Math.Abs((nowLocal.TimeOfDay - target).TotalMinutes);
            if (diffMinutes > timeMatchWindowMinutes)
                return false;
        }

        return true;
    }

    public static List<int> ParseMonths(string? csv)
    {
        var result = new List<int>();
        if (string.IsNullOrWhiteSpace(csv))
            return result;

        foreach (var part in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(part, out var m) && m is >= 1 and <= 12)
                result.Add(m);
        }

        return result;
    }
}
