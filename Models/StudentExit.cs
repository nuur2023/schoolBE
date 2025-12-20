namespace schoolBE.Models;

public class StudentExit : BaseModel
{
    public int StudentId { get; set; }
    public int? SchoolId { get; set; }
    public int? CreatorId { get; set; }
    public DateTime ExitDate { get; set; }
    public string ExitType { get; set; } = string.Empty;
    public string? Reason { get; set; }
    
    public StudentEnrollment? Student { get; set; }
    public School? School { get; set; }
    public User? Creator { get; set; }
}

