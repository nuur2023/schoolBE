namespace schoolBE.Models;

public class ExamSubject : BaseModel
{
    public int ExamScheduleId { get; set; }
    public int LevelSubjectId { get; set; }
    public DateTime ExamDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    
    public ExamSchedule? ExamSchedule { get; set; }
    public LevelSubject? LevelSubject { get; set; }
    public List<ExamResult> ExamResults { get; set; } = new();
}


























