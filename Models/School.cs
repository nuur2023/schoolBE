namespace schoolBE.Models;

public class School : BaseModel
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Logo { get; set; }
    public int? Days { get; set; }
    public int CreatorId { get; set; }
    public bool IsActive { get; set; }
}

