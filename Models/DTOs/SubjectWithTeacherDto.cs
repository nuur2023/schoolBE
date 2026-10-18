namespace schoolBE.Models.DTOs;

public class SubjectWithTeacherDto
{
    public string Subject { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty; // Class name if classId provided, else level name
    public string? Teacher { get; set; } // Teacher name if assigned in ClassSubjects, else null
}





