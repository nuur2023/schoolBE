using schoolBE.Models;

namespace schoolBE.Data.Interfaces;

public interface IStages : IBaseSchoolInterface<Stage>
{
    Task<Stage?> GetByIdAsync(int id);
    Task<List<Stage>> GetAllAsync();
    Task<(List<Stage> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null);
    Task<Stage> ActivateAsync(int id);
}


