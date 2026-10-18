namespace schoolBE.ViewModels;

public class StudentOfTheYearResponseDto
{
    public string SchoolId { get; set; } = string.Empty;
    public string LevelId { get; set; } = string.Empty;
    public string? ClassId { get; set; }
    public string AwardType { get; set; } = "STUDENT_OF_THE_YEAR";
    public string Criteria { get; set; } = "HIGHEST_AVERAGE";
    public bool IsAvailable { get; set; }
    public string GeneratedAt { get; set; } = string.Empty;
    public string AvailableUntil { get; set; } = string.Empty;
    public List<StudentOfTheYearResultDto> Results { get; set; } = new();
}

public class StudentOfTheYearResultDto
{
    public string Scope { get; set; } = string.Empty; // "LEVEL" or "SCHOOL"
    public string ScopeId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Image { get; set; }
    public string FullName { get; set; } = string.Empty;
    public double MaxAverage { get; set; }
    public StudentOfTheYearDetailsDto Details { get; set; } = new();
}

public class StudentOfTheYearDetailsDto
{
    public string StudentId { get; set; } = string.Empty;
    public string StudentLevelId { get; set; } = string.Empty;
    public string StudentClassId { get; set; } = string.Empty;
    public double Average { get; set; }
}


























