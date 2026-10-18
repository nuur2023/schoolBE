namespace schoolBE.ViewModels;

public class ExamHistoryDto
{
    public string AcademicYear { get; set; } = string.Empty;
    public string ExamName { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty; // Format: "Month Year" (e.g., "December 2025")
    public decimal FullMarks { get; set; }
    public decimal Average { get; set; }
    public string Status { get; set; } = string.Empty; // "Passed" if average > 49, otherwise "Failed"
    public List<ExamHistoryResultDto> Results { get; set; } = new();
}

public class ExamHistoryResultDto
{
    public string Subject { get; set; } = string.Empty;
    public decimal Marks { get; set; }
}



