namespace schoolBE.Models;

public class JournalDetail : BaseModel
{
    public int JournalEntryId { get; set; }
    public int SchoolAccountId { get; set; }
    public int SchoolId { get; set; }
    public int CreatorId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
}

