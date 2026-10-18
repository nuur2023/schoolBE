namespace schoolBE.Models.DTOs;

public class StudentAttendanceResponse
{
    public AttendanceSummary Summary { get; set; } = new();
    public List<StudentAttendanceRecord> Attendances { get; set; } = new();
}

public class AttendanceSummary
{
    public double PresentPercentage { get; set; }
    public double AbsentPercentage { get; set; }
}

public class StudentAttendanceRecord
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public bool IsPresent { get; set; }
    public List<AttendanceSubject> AttendanceSubjects { get; set; } = new();
}

public class AttendanceSubject
{
    public int Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public bool IsPresent { get; set; }
    public string? Reason { get; set; }
}









