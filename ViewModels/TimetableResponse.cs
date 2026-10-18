namespace schoolBE.ViewModels;

public class TimetableResponse
{
    public List<TimetableDayDto> Days { get; set; } = new();
}

public class TimetableDayDto
{
    public string Id { get; set; } = string.Empty;
    public string Day { get; set; } = string.Empty;
    public List<TimetableClassDto> Classes { get; set; } = new();
}

public class TimetableClassDto
{
    public string Id { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string PhotoUrl { get; set; } = string.Empty;
    public string Teacher { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
    public bool IsPresent { get; set; }
    public string Color { get; set; } = string.Empty;
}
