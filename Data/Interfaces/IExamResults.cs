using schoolBE.Models;

namespace schoolBE.Data.Interfaces;

public interface IExamResults : IBaseSchoolInterface<ExamResult>
{
    Task<ExamResult?> GetByIdAsync(int id);
    Task<List<ExamResult>> GetAllAsync();
    Task<(List<ExamResult> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null);
    Task<List<ExamResult>> CreateBulkAsync(List<ExamResult> results);
}

