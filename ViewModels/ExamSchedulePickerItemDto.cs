namespace schoolBE.ViewModels;

/// <summary>
/// Lightweight row for exam schedule dropdowns (e.g. Create Participants).
/// </summary>
public class ExamSchedulePickerItemDto
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
}
