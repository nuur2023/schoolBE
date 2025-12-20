using schoolBE.Models;

namespace schoolBE.Data.Interfaces;

public interface IClassSubjects : IBaseSchoolInterface<ClassSubject>
{
    Task<ClassSubject?> GetByIdAsync(int id);
    Task<List<ClassSubject>> GetAllAsync();
    Task<(List<ClassSubject> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null);
}


