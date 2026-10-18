namespace schoolBE.Models.DTOs;

public class StudentExamResultsResponse
{
    public int Id { get; set; }
    public string GenId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Image { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Stage { get; set; }
    public string? Level { get; set; }
    public string? Class { get; set; }
    public int GradeId { get; set; }
    public int LevelId { get; set; }
    public int ClassId { get; set; }
    public List<LevelExamResults> ExamResults { get; set; } = new();
}

public class LevelExamResults
{
    public int LevelId { get; set; }
    public string Level { get; set; } = string.Empty;
    public List<SubjectResultDetail> SubjectResults { get; set; } = new();
    public List<SubjectResultSummary> Results { get; set; } = new();
    public string Average { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal? TotalMarks { get; set; }

}

public class SubjectResultDetail
{
    public int Id { get; set; }
    public string Level { get; set; } = string.Empty;
    public int SubjectId { get; set; }
    public int ClassId { get; set; }
    public string ExamTitle { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public decimal Marks { get; set; }
    public decimal FullMarks { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
    public string Created { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
}

public class SubjectResultSummary
{
    public int Id { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public decimal Marks { get; set; }
    public decimal Percentage { get; set; }
    public string Status { get; set; } = string.Empty;
}



