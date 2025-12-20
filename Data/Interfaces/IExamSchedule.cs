using schoolBE.Models;

namespace schoolBE.Data.Interfaces;

public interface IExamSchedule : IBaseSchoolInterface<ExamSchedule>
{
    Task<ExamSchedule?> GetByIdAsync(int id);
    Task<List<ExamSchedule>> GetAllAsync();
    Task<(List<ExamSchedule> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null);
}


