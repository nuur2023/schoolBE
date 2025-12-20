using schoolBE.Models;

namespace schoolBE.Data.Interfaces;

public interface ILevels : IBaseSchoolInterface<Level>
{
    Task<Level?> GetByIdAsync(int id);
    Task<List<Level>> GetAllAsync();
    Task<(List<Level> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null);
    Task<Level> ActivateAsync(int id);
}


