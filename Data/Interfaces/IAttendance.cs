using schoolBE.Models;
using schoolBE.Models.DTOs;

namespace schoolBE.Data.Interfaces;

public interface IAttendance : IBaseSchoolInterface<Attendance>
{
    Task<Attendance?> GetByIdAsync(int id);
    Task<List<Attendance>> GetAllAsync();
    Task<(List<Attendance> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null);
    Task<List<Attendance>> CreateBulkAsync(List<Attendance> attendances);
    Task<List<Attendance>> GetByFiltersAsync(int? schoolId, int? stageId, int? levelId, int? classId, int? studentId);
    Task<StudentAttendanceResponse> GetStudentAttendancesAsync(int studentId, int schoolId, int levelId, int? months = 2);
}


