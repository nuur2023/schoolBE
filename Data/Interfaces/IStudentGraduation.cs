using schoolBE.Models;

namespace schoolBE.Data.Interfaces;

public interface IStudentGraduation : IBaseSchoolInterface<StudentGraduation>
{
    Task<StudentGraduation?> GetByIdAsync(int id);
    Task<List<StudentGraduation>> GetAllAsync();
    Task<(List<StudentGraduation> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null, int? schoolId = null, int? studentId = null);
}


























