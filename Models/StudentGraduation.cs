namespace schoolBE.Models;

public class StudentGraduation : BaseModel
{
    public int StudentId { get; set; }
    public int? SchoolId { get; set; }
    public int? CreatorId { get; set; }
    public DateTime GraduationDate { get; set; }
    public string? Reason { get; set; }
    
    public StudentEnrollment? Student { get; set; }
    public School? School { get; set; }
    public User? Creator { get; set; }
}


























