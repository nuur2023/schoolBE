namespace schoolBE.Models;

public class ExamRoom : BaseModel
{
    public string Name { get; set; } = string.Empty;
    public int? SchoolId { get; set; }
    public int CreatorId { get; set; }
    public int? Capacity { get; set; }
    
    public School? School { get; set; }
}

