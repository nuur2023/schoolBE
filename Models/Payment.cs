namespace schoolBE.Models;

public class Payment : BaseModel
{
    public string ReferenceNo { get; set; } = string.Empty;
    public string? ReferenceId { get; set; }
    public string Type { get; set; } = string.Empty; // charged, paid, refunded
    public decimal Charged { get; set; }
    public decimal Paid { get; set; }
    public int SchoolId { get; set; }
    public int? StudentId { get; set; }
    // Stored as a name (e.g. cash account name) instead of a FK id
    public string PaymentMethod { get; set; } = string.Empty;
    public int? FeeCategoriesId { get; set; }
    public int CreatorId { get; set; }
    
    // Navigation properties
    public FeeCategories? FeeCategories { get; set; }
    public StudentEnrollment? Student { get; set; }
}
