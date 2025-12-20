using schoolBE.Models;

namespace schoolBE.Data.Interfaces;

public interface IPayment : IBaseSchoolInterface<Payment>
{
    Task<Payment?> GetByIdAsync(int id);
    Task<List<Payment>> GetAllAsync();
    Task<(List<Payment> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null);
}


