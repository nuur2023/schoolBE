namespace schoolBE.Models;

public class ExamResult : BaseModel
{
    public int ExamScheduleId { get; set; }
    public int StudentId { get; set; }
    public int SchoolId { get; set; }
    public int CreatorId { get; set; }
    public int AcademicYear { get; set; }
    public DateTime Date { get; set; }
    public decimal Marks { get; set; }
    public decimal FullMarks { get; set; }
    public decimal Percentage { get; set; }
    public string PublicationStatus { get; set; } = string.Empty;
    
    public ExamSchedule? ExamSchedule { get; set; }
    public StudentEnrollment? StudentEnrollment { get; set; }
}

