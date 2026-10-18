namespace schoolBE.Models.DTOs;

public class ChargeStudentsRequest
{
    public string ChargeType { get; set; } = string.Empty; // "stage", "level", "class", "individual"
    public int SchoolId { get; set; }
    public int? StageId { get; set; }
    public int? LevelId { get; set; }
    public int? ClassId { get; set; }
    public int? StudentId { get; set; }
    public List<FeeCategoryCharge> FeeCategories { get; set; } = new();
    public string? Description { get; set; }
}

public class FeeCategoryCharge
{
    public int FeeCategoryId { get; set; }
    public decimal Amount { get; set; }
}
