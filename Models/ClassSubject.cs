namespace schoolBE.Models;

public class ClassSubject : BaseModel
{
    public int ClassId { get; set; }
    public int SubjectId { get; set; }
    public int? PersonId { get; set; }
    public int CreatorId { get; set; }
    public int? DayOfWeek { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    
    public Class? Class { get; set; }
    public Subject? Subject { get; set; }
    public Person? Person { get; set; }
}

