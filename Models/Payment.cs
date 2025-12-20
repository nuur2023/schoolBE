namespace schoolBE.Models;

public class Payment : BaseModel
{
    public string ReferenceNo { get; set; } = string.Empty;
    public string? ReferenceId { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Charged { get; set; }
    public decimal Paid { get; set; }
    public int SchoolId { get; set; }
    public int? StudentId { get; set; }
    public int? PaymentMethodId { get; set; }
    public int CreatorId { get; set; }
}

