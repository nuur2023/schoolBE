using schoolBE.Models;

namespace schoolBE.Data.Interfaces;

public interface IContacts : IBaseSchoolInterface<Contact>
{
    Task<Contact?> GetByIdAsync(int id);
    Task<List<Contact>> GetAllAsync();
    Task<(List<Contact> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null);
}


