using schoolBE.Models;

namespace schoolBE.Data.Interfaces;

public interface IPersons : IBaseSchoolInterface<Person>
{
    Task<Person?> GetByIdAsync(int id);
    Task<List<Person>> GetAllAsync();
    Task<(List<Person> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null);
    Task<Person> ActivateAsync(int id);
}

