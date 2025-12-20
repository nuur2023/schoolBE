namespace schoolBE.Models;

public class PayRoll : BaseModel
{
    public int PersonId { get; set; }
    public int SchoolId { get; set; }
    public int CreatorId { get; set; }
    public string PayPeriod { get; set; } = string.Empty;
    public decimal NetPaid { get; set; }
}

