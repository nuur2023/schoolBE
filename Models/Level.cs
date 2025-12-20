namespace schoolBE.Models;

public class Level : BaseModel
{
    public string Name { get; set; } = string.Empty;
    public int SchoolId { get; set; }
    public int StageId { get; set; }
    public int CreatorId { get; set; }
    public bool IsActive { get; set; }
    
    public Stage? Stage { get; set; }
}

