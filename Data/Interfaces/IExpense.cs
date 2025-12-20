using schoolBE.Models;

namespace schoolBE.Data.Interfaces;

public interface IExpense : IBaseSchoolInterface<Expense>
{
    Task<Expense?> GetByIdAsync(int id);
    Task<List<Expense>> GetAllAsync();
    Task<(List<Expense> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null);
}


