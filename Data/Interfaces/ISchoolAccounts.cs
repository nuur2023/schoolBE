using schoolBE.Models;

namespace schoolBE.Data.Interfaces;

public interface ISchoolAccounts : IBaseSchoolInterface<SchoolAccount>
{
    Task<SchoolAccount?> GetByIdAsync(int id);
    Task<List<SchoolAccount>> GetAllAsync();
    Task<(List<SchoolAccount> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null);
}


