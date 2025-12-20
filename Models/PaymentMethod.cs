namespace schoolBE.Models;

public class PaymentMethod : BaseModel
{
    public string MethodName { get; set; } = string.Empty;
    public string ProviderMethod { get; set; } = string.Empty;
    public int SchoolId { get; set; }
    public int CreatorId { get; set; }
    public bool IsActive { get; set; }
}

