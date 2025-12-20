namespace schoolBE.Models;

public class LevelSubject : BaseModel
{
    public int LevelId { get; set; }
    public int SubjectId { get; set; }
    public int SchoolId { get; set; }
    public int CreatorId { get; set; }
    
    public Level? Level { get; set; }
    public Subject? Subject { get; set; }
}

