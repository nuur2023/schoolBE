using schoolBE.Models;

namespace schoolBE.Data.Interfaces;

public interface IExamSubject : IBaseSchoolInterface<ExamSubject>
{
    Task<ExamSubject?> GetByIdAsync(int id);
    Task<List<ExamSubject>> GetAllAsync();
    Task<(List<ExamSubject> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null, int? examScheduleId = null, DateTime? examDate = null);
}


























