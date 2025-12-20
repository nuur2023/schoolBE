using schoolBE.Models;
using schoolBE.ViewModels;

namespace schoolBE.Data.Interfaces;

public interface IRoles : IBaseSchoolInterface<Role>
{
    Task<Role?> GetByIdAsync(int id);
    Task<List<Role>> GetAllAsync();
    Task<(List<Role> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null);
    Task<List<RoleVM>> GetRolesAsync();
}

