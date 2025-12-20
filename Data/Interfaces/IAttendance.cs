using schoolBE.Models;

namespace schoolBE.Data.Interfaces;

public interface IAttendance : IBaseSchoolInterface<Attendance>
{
    Task<Attendance?> GetByIdAsync(int id);
    Task<List<Attendance>> GetAllAsync();
    Task<(List<Attendance> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null);
}


