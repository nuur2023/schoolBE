using schoolBE.Models;

namespace schoolBE.Data.Interfaces;

public interface IPayRoll : IBaseSchoolInterface<PayRoll>
{
    Task<PayRoll?> GetByIdAsync(int id);
    Task<List<PayRoll>> GetAllAsync();
    Task<(List<PayRoll> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null);
}


