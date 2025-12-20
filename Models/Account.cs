namespace schoolBE.Models;

public class Account : BaseModel
{
    public string Name { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public int ChartOfAccountId { get; set; }
}

