namespace schoolBE.Models;

public class Attendance : BaseModel
{
    public int SchoolId { get; set; }
    public int StudentId { get; set; }
    public int SubjectId { get; set; }
    public int CreatorId { get; set; }
    public bool IsPresent { get; set; }
    public string? Reason { get; set; }

    // Navigation properties
    public StudentEnrollment? Student { get; set; }
    public Subject? Subject { get; set; }
}

