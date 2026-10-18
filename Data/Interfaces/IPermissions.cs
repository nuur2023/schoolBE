using schoolBE.Models;
using schoolBE.ViewModels;

namespace schoolBE.Data.Interfaces;

public interface IPermissions : IBaseSchoolInterface<Permission>
{
    Task<Permission?> GetByIdAsync(int id);
    Task<List<Permission>> GetAllAsync();
    Task<(List<Permission> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null);
    Task<List<PermissionVM>> GetUserPermissionsAsync(int userId, int schoolId);
    Task SaveUserPermissionsAsync(int userId, int schoolId, List<PermissionVM> permissions);
    Task DeleteForMenuAsync(int menuId);
}

