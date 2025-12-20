using schoolBE.Models;

namespace schoolBE.Data.Interfaces;

public interface IExams : IBaseSchoolInterface<Exam>
{
    Task<Exam?> GetByIdAsync(int id);
    Task<List<Exam>> GetAllAsync();
    Task<(List<Exam> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null);
    Task<Exam> ActivateAsync(int id);
}


