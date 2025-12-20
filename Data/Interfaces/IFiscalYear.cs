using schoolBE.Models;

namespace schoolBE.Data.Interfaces;

public interface IFiscalYear : IBaseSchoolInterface<FiscalYear>
{
    Task<FiscalYear?> GetByIdAsync(int id);
    Task<List<FiscalYear>> GetAllAsync();
    Task<(List<FiscalYear> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null);
}


