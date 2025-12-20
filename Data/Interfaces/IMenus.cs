using schoolBE.Models;

namespace schoolBE.Data.Interfaces;

public interface IMenus : IBaseSchoolInterface<Menu>
{
    Task<Menu?> GetByIdAsync(int id);
    Task<List<Menu>> GetAllAsync();
    Task<(List<Menu> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null);
}


