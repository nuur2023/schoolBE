namespace schoolBE.Models;

public class ExamParticipants : BaseModel
{
    public int ExamScheduleId { get; set; }
    public int StudentId { get; set; }
    public int CreatorId { get; set; }
    public int? ClassId { get; set; }
    public int? LevelId { get; set; }
    public int? RoomId { get; set; }
    public int? SubjectId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? StatusNote { get; set; }
    public DateTime? StatusUpdatedAt { get; set; }
    public string? SeatNumber { get; set; }
    public bool ReceivedMarks { get; set; }
    
    public ExamSchedule? ExamSchedule { get; set; }
    public StudentEnrollment? Student { get; set; }
    public Class? Class { get; set; }
    public Level? Level { get; set; }
    public ExamRoom? Room { get; set; }
    public Subject? Subject { get; set; }
    public User? Creator { get; set; }
}

