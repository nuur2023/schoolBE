namespace schoolBE.ViewModels;

public class ExamScheduleResponse
{
    public List<ExamScheduleDto> Schedules { get; set; } = new();
}

public class ExamScheduleDto
{
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}











