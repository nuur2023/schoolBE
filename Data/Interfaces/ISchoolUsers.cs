using schoolBE.Models;

namespace schoolBE.Data.Interfaces;

public interface ISchoolUsers : IBaseSchoolInterface<SchoolUser>
{
    Task<SchoolUser?> GetByIdAsync(int id);
    Task<List<SchoolUser>> GetAllAsync();
    Task<(List<SchoolUser> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null);
    Task<SchoolUser> ActivateAsync(int id);
}


