using schoolBE.Models;

namespace schoolBE.Data.Interfaces;

public interface IJournalEntry : IBaseSchoolInterface<JournalEntry>
{
    Task<JournalEntry?> GetByIdAsync(int id);
    Task<List<JournalEntry>> GetAllAsync();
    Task<(List<JournalEntry> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null);
}


