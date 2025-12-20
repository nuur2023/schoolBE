using schoolBE.Models;

namespace schoolBE.Data.Interfaces;

public interface ISubmenus : IBaseSchoolInterface<Submenu>
{
    Task<Submenu?> GetByIdAsync(int id);
    Task<List<Submenu>> GetAllAsync();
    Task<(List<Submenu> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null);
}


