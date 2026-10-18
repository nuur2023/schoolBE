using schoolBE.Models;
using schoolBE.Models.DTOs;
using schoolBE.ViewModels;

namespace schoolBE.Data.Interfaces;

public interface IApp
{
    Task<StudentEnrollment?> GetStudentByIdAsync(int studentId);
    Task<StudentEnrollment?> GetStudentByGenIdAsync(string genId);
    Task<StudentEnrollment?> GetStudentByGenIdAndSchoolIdAsync(string genId, int schoolId);
    Task<decimal> GetStudentBalanceAsync(int studentId);
    Task<int> GetStudentSubjectCountAsync(int studentId);
    Task<decimal> GetStudentAttendancePercentageAsync(int studentId, int? month = null);
    Task<decimal> GetStudentBalanceFromJournalEntriesAsync(int studentId);
    Task<(StudentEnrollment? student, List<Contact> contacts)> GetStudentProfileWithContactsAsync(int studentId, int schoolId);
    Task<TimetableResponse> GetTimetableAsync(int studentId, int schoolId, int levelId, int? classId = null);
    Task<List<ExamScheduleDto>> GetStudentExamSchedulesAsync(int studentId, int schoolId, int levelId);
    Task<List<SubjectWithTeacherDto>> GetSubjectsWithTeachersAsync(int schoolId, int studentId, int levelId, int? classId = null);
    Task<List<ExamHistoryDto>> GetExamHistoryAsync(int studentId, int schoolId, int levelId, int? classId = null);
    Task<StudentOfTheYearResponseDto> GetStudentOfTheYearAsync(int schoolId, int levelId, string academicYear);
}

