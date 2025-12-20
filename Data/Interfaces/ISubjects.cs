using schoolBE.Models;

namespace schoolBE.Data.Interfaces;

public interface ISubjects : IBaseSchoolInterface<Subject>
{
    Task<Subject?> GetByIdAsync(int id);
    Task<List<Subject>> GetAllAsync();
    Task<(List<Subject> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null);
    Task<Subject> ActivateAsync(int id);
}


