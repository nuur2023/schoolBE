namespace schoolBE.Models;

public class StudentTransfer : BaseModel
{
    public int StudentId { get; set; }
    public int? SchoolId { get; set; }
    public int? CreatorId { get; set; }
    public DateTime TransferDate { get; set; }
    public string TransferType { get; set; } = string.Empty; // "StageToStage", "LevelToLevel", "ClassToClass"
    
    // From (source)
    public int FromStageId { get; set; }
    public int FromLevelId { get; set; }
    public int FromClassId { get; set; }
    
    // To (destination)
    public int ToStageId { get; set; }
    public int ToLevelId { get; set; }
    public int ToClassId { get; set; }
    
    public string? Reason { get; set; }
    
    public StudentEnrollment? Student { get; set; }
    public School? School { get; set; }
    public User? Creator { get; set; }
    public Stage? FromStage { get; set; }
    public Level? FromLevel { get; set; }
    public Class? FromClass { get; set; }
    public Stage? ToStage { get; set; }
    public Level? ToLevel { get; set; }
    public Class? ToClass { get; set; }
}




















