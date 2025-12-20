namespace schoolBE.Models;

public class SchoolAccount : BaseModel
{
    public string Name { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string SchoolAccountType { get; set; } = string.Empty;
    public int AccountId { get; set; }
    public int SchoolId { get; set; }
    public int CreatorId { get; set; }
}

