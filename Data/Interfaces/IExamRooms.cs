using schoolBE.Models;

namespace schoolBE.Data.Interfaces;

public interface IExamRooms : IBaseSchoolInterface<ExamRoom>
{
    Task<ExamRoom?> GetByIdAsync(int id);
    Task<List<ExamRoom>> GetAllAsync();
    Task<(List<ExamRoom> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null);
}













