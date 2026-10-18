using schoolBE.Models;

namespace schoolBE.Data.Interfaces;

public interface IStudentTransfer : IBaseSchoolInterface<StudentTransfer>
{
    Task<StudentTransfer?> GetByIdAsync(int id);
    Task<List<StudentTransfer>> GetAllAsync();
    Task<(List<StudentTransfer> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null, int? schoolId = null, int? studentId = null);
    Task<List<StudentTransfer>> GetByStudentIdAsync(int studentId);
}




















