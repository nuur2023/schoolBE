using System.Text.Json.Serialization;

namespace schoolBE.ViewModels;

public class StudentDashboardResponse
{
    [JsonPropertyName("student_info")]
    public StudentInfoDto StudentInfo { get; set; } = new();
    
    [JsonPropertyName("attendance")]
    public AttendanceDto Attendance { get; set; } = new();
    
    [JsonPropertyName("subjects")]
    public SubjectsDto Subjects { get; set; } = new();
    
    [JsonPropertyName("balance")]
    public BalanceDto Balance { get; set; } = new();
}

public class StudentInfoDto
{
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("genId")]
    public string GenId { get; set; } = string.Empty;
    
    public string Level { get; set; } = string.Empty;
    
    public string Grade { get; set; } = string.Empty;
    
    [JsonPropertyName("photo_url")]
    public string PhotoUrl { get; set; } = string.Empty;
    
    [JsonPropertyName("isActiveInApp")]
    public bool IsActiveInApp { get; set; }
}

public class AttendanceDto
{
    public string Name { get; set; } = "Monthly";
    
    [JsonPropertyName("sub_name")]
    public string SubName { get; set; } = "Attendance";
    
    [JsonPropertyName("attendancePercentage")]
    public decimal AttendancePercentage { get; set; }
}

public class SubjectsDto
{
    public string Name { get; set; } = "Class";
    
    [JsonPropertyName("sub_name")]
    public string SubName { get; set; } = "subjects";
    
    public int Number { get; set; }
}

public class BalanceDto
{
    public string Name { get; set; } = "Student";
    
    [JsonPropertyName("sub_name")]
    public string SubName { get; set; } = "Balance";
    
    public decimal Amount { get; set; }
}

