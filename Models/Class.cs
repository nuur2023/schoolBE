namespace schoolBE.Models;

public class Class : BaseModel
{
    public string Name { get; set; } = string.Empty;
    public int SchoolId { get; set; }
    public int LevelId { get; set; }
    public int CreatorId { get; set; }
    public bool IsActive { get; set; }
    
    public Level? Level { get; set; }
}

