using schoolBE.Models;

namespace schoolBE.Data.Interfaces;

public interface IStudentExit : IBaseSchoolInterface<StudentExit>
{
    Task<StudentExit?> GetByIdAsync(int id);
    Task<List<StudentExit>> GetAllAsync();
    Task<(List<StudentExit> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null, int? schoolId = null, int? studentId = null);
}


























