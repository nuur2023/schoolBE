namespace schoolBE.Models;

public class Expense : BaseModel
{
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int? SchoolId { get; set; }
    public int CreatorId { get; set; }
}

