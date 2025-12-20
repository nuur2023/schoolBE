using schoolBE.Models;

namespace schoolBE.Data.Interfaces;

public interface IChartOfAccount : IBaseSchoolInterface<ChartOfAccount>
{
    Task<ChartOfAccount?> GetByIdAsync(int id);
    Task<List<ChartOfAccount>> GetAllAsync();
    Task<(List<ChartOfAccount> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null);
}


