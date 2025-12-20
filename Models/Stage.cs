namespace schoolBE.Models;

public class Stage : BaseModel
{
    public string Name { get; set; } = string.Empty;
    public int SchoolId { get; set; }
    public int CreatorId { get; set; }
    public bool IsActive { get; set; }
}

