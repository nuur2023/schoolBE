namespace schoolBE.ViewModels;

public class StudentPaymentDetailsResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string GenId { get; set; } = string.Empty;
    public string? Level { get; set; }
    public string? Class { get; set; }
    public decimal UnpaidBalance { get; set; }
}













