namespace schoolBE.Models;

public class Exam : BaseModel
{
    public string Title { get; set; } = string.Empty;
    public int SchoolId { get; set; }
    public int CreatorId { get; set; }
    public bool IsActive { get; set; }
    public decimal? Weight { get; set; }
    
    public School? School { get; set; }
}

