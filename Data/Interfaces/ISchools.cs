using schoolBE.Models;

namespace schoolBE.Data.Interfaces;

public interface ISchools : IBaseSchoolInterface<School>
{
    Task<School?> GetByIdAsync(int id);
    Task<List<School>> GetAllAsync();
    Task<(List<School> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null);
    Task<School> ActivateAsync(int id);
}

