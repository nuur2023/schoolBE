using schoolBE.Models;

namespace schoolBE.Data.Interfaces;

public interface IAccounts : IBaseSchoolInterface<Account>
{
    Task<Account?> GetByIdAsync(int id);
    Task<List<Account>> GetAllAsync();
    Task<(List<Account> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null);
}


