using schoolBE.Models;

namespace schoolBE.Data.Interfaces;

public interface IYearEndClosingEntry : IBaseSchoolInterface<YearEndClosingEntry>
{
    Task<YearEndClosingEntry?> GetByIdAsync(int id);
    Task<List<YearEndClosingEntry>> GetAllAsync();
    Task<(List<YearEndClosingEntry> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null);
}


