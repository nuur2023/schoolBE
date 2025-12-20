using schoolBE.Models;

namespace schoolBE.Data.Interfaces;

public interface IClasses : IBaseSchoolInterface<Class>
{
    Task<Class?> GetByIdAsync(int id);
    Task<List<Class>> GetAllAsync();
    Task<(List<Class> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null);
    Task<Class> ActivateAsync(int id);
}


