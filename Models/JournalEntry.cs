namespace schoolBE.Models;

public class JournalEntry : BaseModel
{
    public string ReferenceNo { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public int BusinessId { get; set; }
    public int? UserId { get; set; }
    public string? SourceType { get; set; }
    public int? SourceId { get; set; }
}

