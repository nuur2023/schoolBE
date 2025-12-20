using schoolBE.Models;

namespace schoolBE.Data.Interfaces;

public interface IStudentEnrollments : IBaseSchoolInterface<StudentEnrollment>
{
    Task<StudentEnrollment?> GetByIdAsync(int id);
    Task<List<StudentEnrollment>> GetAllAsync();
    Task<(List<StudentEnrollment> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null);
    Task<StudentEnrollment> ActivateAsync(int id);
}


