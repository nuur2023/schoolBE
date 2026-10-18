namespace schoolBE.Models;

public class ExamResult : BaseModel
{
    public int ExamSubjectId { get; set; }
    public int StudentId { get; set; }
    public int SchoolId { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
    public decimal Marks { get; set; }
    public decimal FullMarks { get; set; }
    public decimal Percentage { get; set; }
    public ExamSubject? ExamSubject { get; set; }
    public StudentEnrollment? StudentEnrollment { get; set; }
    public DateTime Date { get; set; }
    public string PublicationStatus { get; set; } = string.Empty;
}

