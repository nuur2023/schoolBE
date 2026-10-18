using schoolBE.Models;
using schoolBE.ViewModels;

namespace schoolBE.Data.Interfaces;

public interface IExamSchedule : IBaseSchoolInterface<ExamSchedule>
{
    Task<ExamSchedule?> GetByIdAsync(int id);
    Task<List<ExamSchedule>> GetAllAsync();
    Task<(List<ExamSchedule> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null);

    /// <summary>
    /// Active exam schedules for a school (Create Participants — no level/subject filter on the list).
    /// </summary>
    Task<List<ExamSchedulePickerItemDto>> GetActiveSchedulesPickerForSchoolAsync(int schoolId);
}


