using Microsoft.EntityFrameworkCore;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using schoolBE.Models;
using schoolBE.Models.DTOs;

namespace schoolBE.Data.Repositories;

public class AttendanceRepository(SchoolContext context) : IAttendance
{
    private readonly SchoolContext _context = context;
    private readonly DbSet<Attendance> _dbSet = context.Set<Attendance>();

    public async Task<Attendance> AddAsync(Attendance entity)
    {
        var todayUtc = DateTime.UtcNow.Date;
        var tomorrowUtc = todayUtc.AddDays(1);

        var existing = await _dbSet.FirstOrDefaultAsync(a =>
            a.SchoolId == entity.SchoolId &&
            a.StudentId == entity.StudentId &&
            a.SubjectId == entity.SubjectId &&
            a.CreatedAt >= todayUtc &&
            a.CreatedAt < tomorrowUtc);

        if (existing != null)
            return existing;

        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Attendance> UpdateAsync(Attendance entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Attendance> DeleteAsync(Attendance entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Attendance?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<List<Attendance>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<(List<Attendance> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null)
    {
        var query = _dbSet.AsQueryable();

        var totalCount = await query.CountAsync();
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (data, totalCount);
    }

    public async Task<List<Attendance>> CreateBulkAsync(List<Attendance> attendances)
    {
        if (attendances == null || attendances.Count == 0)
            return new List<Attendance>();

        // One row per (school, student, subject) in this request
        var dedupedIncoming = attendances
            .GroupBy(a => (a.SchoolId, a.StudentId, a.SubjectId))
            .Select(g => g.First())
            .ToList();

        var todayUtc = DateTime.UtcNow.Date;
        var tomorrowUtc = todayUtc.AddDays(1);

        var schoolIds = dedupedIncoming.Select(a => a.SchoolId).Distinct().ToList();
        var studentIds = dedupedIncoming.Select(a => a.StudentId).Distinct().ToList();
        var subjectIds = dedupedIncoming.Select(a => a.SubjectId).Distinct().ToList();

        var existingRows = await _dbSet
            .AsNoTracking()
            .Where(a =>
                schoolIds.Contains(a.SchoolId) &&
                studentIds.Contains(a.StudentId) &&
                subjectIds.Contains(a.SubjectId) &&
                a.CreatedAt >= todayUtc &&
                a.CreatedAt < tomorrowUtc)
            .Select(a => new { a.SchoolId, a.StudentId, a.SubjectId })
            .ToListAsync();

        var existingSet = existingRows
            .Select(x => (x.SchoolId, x.StudentId, x.SubjectId))
            .ToHashSet();

        var toAdd = dedupedIncoming
            .Where(a => !existingSet.Contains((a.SchoolId, a.StudentId, a.SubjectId)))
            .ToList();

        if (toAdd.Count == 0)
            return new List<Attendance>();

        await _dbSet.AddRangeAsync(toAdd);
        await _context.SaveChangesAsync();
        return toAdd;
    }

    public async Task<List<Attendance>> GetByFiltersAsync(int? schoolId, int? stageId, int? levelId, int? classId, int? studentId)
    {
        var query = _dbSet.AsQueryable();

        if (schoolId.HasValue)
        {
            query = query.Where(a => a.SchoolId == schoolId.Value);
        }

        if (studentId.HasValue)
        {
            query = query.Where(a => a.StudentId == studentId.Value);
        }
        else if (stageId.HasValue || levelId.HasValue || classId.HasValue)
        {
            // Filter by stage, level, class through student enrollment
            var studentsQuery = _context.Set<StudentEnrollment>().AsQueryable();
            
            if (stageId.HasValue)
            {
                studentsQuery = studentsQuery.Where(s => s.StageId == stageId.Value);
            }
            if (levelId.HasValue)
            {
                studentsQuery = studentsQuery.Where(s => s.LevelId == levelId.Value);
            }
            if (classId.HasValue)
            {
                studentsQuery = studentsQuery.Where(s => s.ClassId == classId.Value);
            }

            var studentIds = await studentsQuery.Select(s => s.Id).ToListAsync();
            if (studentIds.Count > 0)
            {
                query = query.Where(a => studentIds.Contains(a.StudentId));
            }
            else
            {
                // No students match the filters, return empty list
                return new List<Attendance>();
            }
        }

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<StudentAttendanceResponse> GetStudentAttendancesAsync(int studentId, int schoolId, int levelId, int? months = 2)
    {
        // Calculate date range (last N months)
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddMonths(-(months ?? 2));

        // Get student information and verify they belong to the specified school and level
        var student = await _context.Set<StudentEnrollment>()
            .FirstOrDefaultAsync(s => s.Id == studentId && 
                                     s.SchoolId == schoolId && 
                                     s.LevelId == levelId);

        if (student == null)
        {
            return new StudentAttendanceResponse();
        }

        // Get all attendances for this student within the date range, filtered by school and level
        var attendances = await _dbSet
            .Include(a => a.Subject)
            .Where(a => a.StudentId == studentId && 
                       a.SchoolId == schoolId &&
                       a.CreatedAt >= startDate && 
                       a.CreatedAt <= endDate)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        // Group attendances by date (using CreatedAt date only, ignoring time)
        var groupedByDate = attendances
            .GroupBy(a => a.CreatedAt.Date)
            .OrderByDescending(g => g.Key)
            .ToList();

        // Calculate summary percentages based on individual subject attendances (not days)
        var totalSubjectAttendances = attendances.Count;
        var presentSubjectAttendances = attendances.Count(a => a.IsPresent);
        var absentSubjectAttendances = attendances.Count(a => !a.IsPresent);

        var attendanceRecords = new List<StudentAttendanceRecord>();
        var recordId = 1;

        foreach (var dateGroup in groupedByDate)
        {
            var date = dateGroup.Key;
            var dateAttendances = dateGroup.ToList();

            // Determine if student is present for the day
            // Student is considered present if they have at least one subject marked as present
            var isPresentForDay = dateAttendances.Any(a => a.IsPresent);

            // Build attendance subjects list
            var attendanceSubjects = dateAttendances.Select(a => new AttendanceSubject
            {
                Id = a.Id,
                Subject = a.Subject?.Name ?? "Unknown",
                IsPresent = a.IsPresent,
                Reason = a.IsPresent ? null : (a.Reason ?? "unknown")
            }).ToList();

            attendanceRecords.Add(new StudentAttendanceRecord
            {
                Id = recordId++,
                Name = student.Name,
                Date = date.ToString("yyyy-MM-d"), // Format: 2025-07-1 (single digit day)
                IsPresent = isPresentForDay,
                AttendanceSubjects = attendanceSubjects
            });
        }

        // Calculate percentages based on subject attendances
        var presentPercentage = totalSubjectAttendances > 0 
            ? Math.Round((double)presentSubjectAttendances / totalSubjectAttendances * 100, 1) 
            : 0;
        var absentPercentage = totalSubjectAttendances > 0 
            ? Math.Round((double)absentSubjectAttendances / totalSubjectAttendances * 100, 1) 
            : 0;

        return new StudentAttendanceResponse
        {
            Summary = new AttendanceSummary
            {
                PresentPercentage = presentPercentage,
                AbsentPercentage = absentPercentage
            },
            Attendances = attendanceRecords
        };
    }
}
