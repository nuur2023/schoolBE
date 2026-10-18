using schoolBE.Models;
using schoolBE.Models.DTOs;

namespace schoolBE.Data.Interfaces;

public interface IExamResults : IBaseSchoolInterface<ExamResult>
{
    Task<ExamResult?> GetByIdAsync(int id);
    Task<List<ExamResult>> GetAllAsync();
    Task<(List<ExamResult> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null, int? schoolId = null, int? stageId = null, int? levelId = null, int? classId = null, int? examId = null, string? publicationStatus = null);
    Task<List<ExamResult>> CreateBulkAsync(List<ExamResult> results);
    Task<StudentExamResultsResponse?> GetStudentExamResultsAsync(int studentId, int schoolId);
    Task<int> BulkUpdatePublicationStatusAsync(
        int? schoolId,
        int? stageId,
        int? levelId,
        int? classId,
        int? examId,
        string publicationStatus,
        string? currentPublicationStatus = null,
        IReadOnlyList<int>? studentIds = null);
}

