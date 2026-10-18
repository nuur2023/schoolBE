namespace schoolBE.Models;

public class ExamSchedule : BaseModel
{
    public int ExamId { get; set; }
    public int SchoolId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }   // exam period
    public DateTime EndDate { get; set; }
    public bool IsPublished { get; set; }
    public bool IsActive { get; set; } = true;
    
    public Exam? Exam { get; set; }
    public List<ExamSubject> ExamSubjects { get; set; } = new();
}

