using schoolBE.Models;

namespace schoolBE.Data.Interfaces;

public interface IJournalDetails : IBaseSchoolInterface<JournalDetail>
{
    Task<JournalDetail?> GetByIdAsync(int id);
    Task<List<JournalDetail>> GetAllAsync();
    Task<(List<JournalDetail> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null);
}


