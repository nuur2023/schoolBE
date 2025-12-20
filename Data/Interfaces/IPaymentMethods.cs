using schoolBE.Models;

namespace schoolBE.Data.Interfaces;

public interface IPaymentMethods : IBaseSchoolInterface<PaymentMethod>
{
    Task<PaymentMethod?> GetByIdAsync(int id);
    Task<List<PaymentMethod>> GetAllAsync();
    Task<(List<PaymentMethod> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null);
    Task<PaymentMethod> ActivateAsync(int id);
}


