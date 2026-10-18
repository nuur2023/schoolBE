namespace schoolBE.Models.DTOs;

public class AutoChargeSettingWriteDto
{
    public bool IsAllowedAutoCharge { get; set; }
    public string MonthsMode { get; set; } = "all";
    public string? SpecificMonths { get; set; }
    public int DayOfMonth { get; set; } = 1;
    public string? ChargeTimeLocal { get; set; }
    public decimal Amount { get; set; }
    public int StageId { get; set; }
    public int FeeCategoryId { get; set; }
    public bool IsActive { get; set; } = true;
    public int SchoolId { get; set; }
}
