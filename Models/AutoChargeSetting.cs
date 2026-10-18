namespace schoolBE.Models;

/// <summary>
/// School-level rule for scheduled automatic student charges (by stage / month / day / optional time).
/// </summary>
public class AutoChargeSetting : BaseModel
{
    /// <summary>When false, the scheduler and API will not post charges for this rule.</summary>
    public bool IsAllowedAutoCharge { get; set; }

    /// <summary>"all" or "specific"</summary>
    public string MonthsMode { get; set; } = "all";

    /// <summary>Comma-separated month numbers 1-12 when MonthsMode is "specific", e.g. "1,3,9".</summary>
    public string? SpecificMonths { get; set; }

    /// <summary>Calendar day of month (1-31) when the charge should run.</summary>
    public int DayOfMonth { get; set; } = 1;

    /// <summary>Optional local time in 24h "HH:mm" (e.g. "14:30"). Null = any time on that day (once).</summary>
    public string? ChargeTimeLocal { get; set; }

    public decimal Amount { get; set; }

    public int StageId { get; set; }

    public int FeeCategoryId { get; set; }

    /// <summary>When false, this rule is ignored entirely.</summary>
    public bool IsActive { get; set; } = true;

    public int SchoolId { get; set; }

    public int CreatorId { get; set; }

    /// <summary>Local calendar date (date component only) of the last successful automatic run.</summary>
    public DateTime? LastRunOn { get; set; }
}
