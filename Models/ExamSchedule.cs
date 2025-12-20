namespace schoolBE.Models;

public class ExamSchedule : BaseModel
{
    public int ExamId { get; set; }
    public int LevelSubjectId { get; set; }
    public int SchoolId { get; set; }
    public int CreatorId { get; set; }
    public DateTime ExamDate { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsActive { get; set; }
    public bool IsPublished { get; set; }
    
    public Exam? Exam { get; set; }
    public LevelSubject? LevelSubject { get; set; }
}

