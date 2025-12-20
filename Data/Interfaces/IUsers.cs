using schoolBE.Models;

namespace schoolBE.Data.Interfaces;

public interface IUsers : IBaseSchoolInterface<User>
{
    Task<User?> GetByIdAsync(int id);
    Task<List<User>> GetAllAsync();
    Task<(List<User> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null);
    Task<User> ActivateAsync(int id);
    Task<User?> GetByEmailAsync(string email);
}

