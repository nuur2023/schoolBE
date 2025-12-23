using schoolBE.Models;

namespace schoolBE.Data.Interfaces;

public interface IExamParticipants : IBaseSchoolInterface<ExamParticipants>
{
    Task<ExamParticipants?> GetByIdAsync(int id);
    Task<List<ExamParticipants>> GetAllAsync();
    Task<(List<ExamParticipants> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null);
    Task<List<ExamParticipants>> GetByExamScheduleIdAsync(int examScheduleId);
    Task<List<int>> GetParticipatedStudentIdsAsync(int examScheduleId);
}

