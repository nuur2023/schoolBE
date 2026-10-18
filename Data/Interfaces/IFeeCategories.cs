using schoolBE.Models;

namespace schoolBE.Data.Interfaces;

public interface IFeeCategories : IBaseSchoolInterface<FeeCategories>
{
    Task<FeeCategories?> GetByIdAsync(int id);
    Task<List<FeeCategories>> GetAllAsync();
    Task<(List<FeeCategories> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null);
}

















