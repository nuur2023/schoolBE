namespace schoolBE.Models;

public class StudentEnrollment : BaseModel
{
    public string Name { get; set; } = string.Empty;
    public string GenId { get; set; } = string.Empty;
    public DateTime DOB { get; set; }
    public string POB { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Mother { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public DateTime? PasswordUpdatedDate { get; set; }
    public string? Photo { get; set; }
    public int SchoolId { get; set; }
    public int StageId { get; set; }
    public int LevelId { get; set; }
    public int ClassId { get; set; }
    public string CurrentAcademicYear { get; set; } = string.Empty;
    public int CreatorId { get; set; }
    public bool IsActive { get; set; }
    public bool IsActiveInApp { get; set; }
    public bool IsGraduated { get; set; }
    public bool IsLeft { get; set; }
    public DateTime? GraduatedDate { get; set; }
    /// <summary>Fixed amount subtracted from gross fee totals before posting charges (manual and automatic).</summary>
    public decimal Discount { get; set; }

    public School? School { get; set; }
    public Stage? Stage { get; set; }
    public Level? Level { get; set; }
    public Class? Class { get; set; }
}
