using schoolBE.Models;

namespace schoolBE.Data.Interfaces;

public interface IStudentEnrollments : IBaseSchoolInterface<StudentEnrollment>
{
    Task<StudentEnrollment?> GetByIdAsync(int id);
    Task<List<StudentEnrollment>> GetAllAsync();
    Task<(List<StudentEnrollment> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null, bool? isActive = null);
    Task<StudentEnrollment> ActivateAsync(int id);
    Task<List<StudentEnrollment>> GetByStageLevelClassAsync(int? stageId, int? levelId, int? classId, List<int>? excludeStudentIds = null);
}


