using Microsoft.EntityFrameworkCore;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using schoolBE.Models;
using schoolBE.Models.DTOs;

namespace schoolBE.Data.Repositories;

public class ExamResultsRepository(SchoolContext context) : IExamResults
{
    private readonly SchoolContext _context = context;
    private readonly DbSet<ExamResult> _dbSet = context.Set<ExamResult>();

    public async Task<ExamResult> AddAsync(ExamResult entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<ExamResult> UpdateAsync(ExamResult entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<ExamResult> DeleteAsync(ExamResult entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<ExamResult?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(er => er.ExamSubject)
                .ThenInclude(es => es!.ExamSchedule)
                    .ThenInclude(s => s!.Exam)
            .Include(er => er.ExamSubject)
                .ThenInclude(es => es!.LevelSubject)
                    .ThenInclude(ls => ls!.Level)
            .Include(er => er.ExamSubject)
                .ThenInclude(es => es!.LevelSubject)
                    .ThenInclude(ls => ls!.Subject)
            .Include(er => er.StudentEnrollment)
                .ThenInclude(se => se!.School)
            .Include(er => er.StudentEnrollment)
                .ThenInclude(se => se!.Stage)
            .Include(er => er.StudentEnrollment)
                .ThenInclude(se => se!.Level)
            .Include(er => er.StudentEnrollment)
                .ThenInclude(se => se!.Class)
            .FirstOrDefaultAsync(er => er.Id == id);
    }

    public async Task<List<ExamResult>> GetAllAsync()
    {
        return await _dbSet
            .Include(er => er.ExamSubject)
                .ThenInclude(es => es!.ExamSchedule)
                    .ThenInclude(s => s!.Exam)
            .Include(er => er.ExamSubject)
                .ThenInclude(es => es!.LevelSubject)
                    .ThenInclude(ls => ls!.Level)
            .Include(er => er.ExamSubject)
                .ThenInclude(es => es!.LevelSubject)
                    .ThenInclude(ls => ls!.Subject)
            .Include(er => er.StudentEnrollment)
                .ThenInclude(se => se!.School)
            .Include(er => er.StudentEnrollment)
                .ThenInclude(se => se!.Stage)
            .Include(er => er.StudentEnrollment)
                .ThenInclude(se => se!.Level)
            .Include(er => er.StudentEnrollment)
                .ThenInclude(se => se!.Class)
            .ToListAsync();
    }

    public async Task<(List<ExamResult> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null, int? schoolId = null, int? stageId = null, int? levelId = null, int? classId = null, int? examId = null, string? publicationStatus = null)
    {
        var query = _dbSet
            .Include(er => er.ExamSubject)
                .ThenInclude(es => es!.ExamSchedule)
                    .ThenInclude(s => s!.Exam)
            .Include(er => er.ExamSubject)
                .ThenInclude(es => es!.LevelSubject)
                    .ThenInclude(ls => ls!.Level)
            .Include(er => er.ExamSubject)
                .ThenInclude(es => es!.LevelSubject)
                    .ThenInclude(ls => ls!.Subject)
            .Include(er => er.StudentEnrollment)
                .ThenInclude(se => se!.School)
            .Include(er => er.StudentEnrollment)
                .ThenInclude(se => se!.Stage)
            .Include(er => er.StudentEnrollment)
                .ThenInclude(se => se!.Level)
            .Include(er => er.StudentEnrollment)
                .ThenInclude(se => se!.Class)
            .AsQueryable();

        // Apply filters
        if (schoolId.HasValue && schoolId.Value > 0)
        {
            query = query.Where(er => er.SchoolId == schoolId.Value);
        }

        if (stageId.HasValue && stageId.Value > 0)
        {
            query = query.Where(er => er.StudentEnrollment != null && er.StudentEnrollment.StageId == stageId.Value);
        }

        if (levelId.HasValue && levelId.Value > 0)
        {
            query = query.Where(er => er.StudentEnrollment != null && er.StudentEnrollment.LevelId == levelId.Value);
        }

        if (classId.HasValue && classId.Value > 0)
        {
            query = query.Where(er => er.StudentEnrollment != null && er.StudentEnrollment.ClassId == classId.Value);
        }

        if (examId.HasValue && examId.Value > 0)
        {
            query = query.Where(er => er.ExamSubject != null && 
                                      er.ExamSubject.ExamSchedule != null && 
                                      er.ExamSubject.ExamSchedule.Exam != null && 
                                      er.ExamSubject.ExamSchedule.Exam.Id == examId.Value);
        }

        if (!string.IsNullOrWhiteSpace(publicationStatus))
        {
            query = query.Where(er => er.PublicationStatus == publicationStatus || 
                                      er.PublicationStatus.ToLower() == publicationStatus.ToLower());
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(er =>
                (er.StudentEnrollment != null && er.StudentEnrollment.Name != null && er.StudentEnrollment.Name.ToLower().Contains(searchLower)) ||
                (er.StudentEnrollment != null && er.StudentEnrollment.GenId != null && er.StudentEnrollment.GenId.ToLower().Contains(searchLower)) ||
                (er.ExamSubject != null && er.ExamSubject.ExamSchedule != null && er.ExamSubject.ExamSchedule.Exam != null && er.ExamSubject.ExamSchedule.Exam.Title != null && er.ExamSubject.ExamSchedule.Exam.Title.ToLower().Contains(searchLower)) ||
                (er.ExamSubject != null && er.ExamSubject.LevelSubject != null && er.ExamSubject.LevelSubject.Subject != null && er.ExamSubject.LevelSubject.Subject.Name != null && er.ExamSubject.LevelSubject.Subject.Name.ToLower().Contains(searchLower)) ||
                er.PublicationStatus.ToLower().Contains(searchLower) ||
                er.Marks.ToString().Contains(search) ||
                er.FullMarks.ToString().Contains(search) ||
                er.Percentage.ToString().Contains(search)
            );
        }

        var totalCount = await query.CountAsync();
        var data = await query
            .OrderByDescending(er => er.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (data, totalCount);
    }

    public async Task<List<ExamResult>> CreateBulkAsync(List<ExamResult> results)
    {
        await _dbSet.AddRangeAsync(results);
        await _context.SaveChangesAsync();
        return results;
    }

    public async Task<StudentExamResultsResponse?> GetStudentExamResultsAsync(int studentId, int schoolId)
    {
        // Get student information
        var student = await _context.Set<StudentEnrollment>()
            .Include(s => s.School)
            .Include(s => s.Stage)
            .Include(s => s.Level)
            .Include(s => s.Class)
            .FirstOrDefaultAsync(s => s.Id == studentId && s.SchoolId == schoolId);

        if (student == null)
        {
            return null;
        }

        // Get all published exam results for this student with matching academic year
        // Note: If exam results are not showing, ensure PublicationStatus is set to "Published" in the web frontend
        // For now, we also include results with empty PublicationStatus to handle legacy data
        var examResults = await _dbSet
            .Include(er => er.ExamSubject)
                .ThenInclude(es => es!.ExamSchedule)
                    .ThenInclude(s => s!.Exam)
            .Include(er => er.ExamSubject)
                .ThenInclude(es => es!.LevelSubject)
                    .ThenInclude(ls => ls!.Level)
            .Include(er => er.ExamSubject)
                .ThenInclude(es => es!.LevelSubject)
                    .ThenInclude(ls => ls!.Subject)
            .Include(er => er.StudentEnrollment)
                .ThenInclude(se => se!.Class)
            .Where(er => er.StudentId == studentId &&
                        er.SchoolId == schoolId &&
                        (er.PublicationStatus == "Published" || er.PublicationStatus == "published" || string.IsNullOrEmpty(er.PublicationStatus)) &&
                        er.AcademicYear == student.CurrentAcademicYear)
            .OrderBy(er => er.ExamSubject!.LevelSubject!.LevelId)
            .ThenBy(er => er.ExamSubject!.LevelSubject!.Subject!.Name)
            .ToListAsync();

        if (examResults.Count == 0)
        {
            // Return student info with empty exam results
            return new StudentExamResultsResponse
            {
                Id = student.Id,
                GenId = student.GenId,
                Name = student.Name,
                Image = student.Photo,
                AcademicYear = student.CurrentAcademicYear,
                Status = student.IsActive ? "Active" : "Inactive",
                Stage = student.Stage?.Name,
                Level = student.Level?.Name,
                Class = student.Class?.Name,
                GradeId = student.StageId,
                LevelId = student.LevelId,
                ClassId = student.ClassId,
                ExamResults = new List<LevelExamResults>()
            };
        }

        // Group results by level
        var groupedByLevel = examResults
            .Where(er => er.ExamSubject?.LevelSubject?.Level != null)
            .GroupBy(er => new
            {
                LevelId = er.ExamSubject!.LevelSubject!.LevelId,
                LevelName = er.ExamSubject!.LevelSubject!.Level!.Name
            })
            .ToList();

        var levelExamResults = new List<LevelExamResults>();

        foreach (var levelGroup in groupedByLevel)
        {
            var levelResults = levelGroup.ToList();
            var levelId = levelGroup.Key.LevelId;
            var levelName = levelGroup.Key.LevelName;

            // Build subjectResults (full details)
            var subjectResults = levelResults.Select(er => new SubjectResultDetail
            {
                Id = er.Id,
                Level = levelName,
                SubjectId = er.ExamSubject!.LevelSubject!.SubjectId,
                ClassId = er.StudentEnrollment?.ClassId ?? 0,
                ExamTitle = er.ExamSubject!.ExamSchedule!.Exam?.Title ?? "Unknown Exam",
                Subject = er.ExamSubject!.LevelSubject!.Subject?.Name ?? "Unknown Subject",
                Marks = er.Marks,
                FullMarks = er.FullMarks,
                AcademicYear = er.AcademicYear,
                Percentage = er.Percentage,
                Created = er.CreatedAt.ToString("yyyy-MM-dd"),
                Date = er.Date.ToString("yyyy-MM-dd")
            }).ToList();

            // Build results (simplified summary)
            var results = levelResults.Select(er => new SubjectResultSummary
            {
                Id = er.Id,
                SubjectName = er.ExamSubject!.LevelSubject!.Subject?.Name ?? "Unknown Subject",
                Marks = er.Marks,
                Percentage = er.Percentage,
                Status = GetStatusFromPercentage(er.Percentage)
            }).ToList();

            // Calculate average percentage
            var averagePercentage = levelResults.Any() 
                ? levelResults.Average(er => (double)er.Percentage) 
                : 0;

            levelExamResults.Add(new LevelExamResults
            {
                LevelId = levelId,
                Level = levelName,
                SubjectResults = subjectResults,
                TotalMarks = subjectResults.Any() ? subjectResults.Sum(x => x.Marks) : 0,
                Results = results,
                Average = $"{averagePercentage:F2}%",
                Status = GetStatusFromPercentage((decimal)averagePercentage)
            });
        }

        return new StudentExamResultsResponse
        {
            Id = student.Id,
            GenId = student.GenId,
            Name = student.Name,
            Image = student.Photo,
            AcademicYear = student.CurrentAcademicYear,
            Status = student.IsActive ? "Active" : "Inactive",
            Stage = student.Stage?.Name,
            Level = student.Level?.Name,
            Class = student.Class?.Name,
            GradeId = student.StageId,
            LevelId = student.LevelId,
            ClassId = student.ClassId,
            ExamResults = levelExamResults
        };
    }

    public async Task<int> BulkUpdatePublicationStatusAsync(
        int? schoolId,
        int? stageId,
        int? levelId,
        int? classId,
        int? examId,
        string publicationStatus,
        string? currentPublicationStatus = null,
        IReadOnlyList<int>? studentIds = null)
    {
        var query = _dbSet
            .Include(er => er.StudentEnrollment)
                .ThenInclude(se => se!.Stage)
            .Include(er => er.StudentEnrollment)
                .ThenInclude(se => se!.Level)
            .Include(er => er.StudentEnrollment)
                .ThenInclude(se => se!.Class)
            .Include(er => er.ExamSubject)
                .ThenInclude(es => es!.ExamSchedule)
                    .ThenInclude(s => s!.Exam)
            .AsQueryable();

        if (schoolId.HasValue)
        {
            query = query.Where(er => er.SchoolId == schoolId.Value);
        }

        if (stageId.HasValue)
        {
            query = query.Where(er => er.StudentEnrollment != null && er.StudentEnrollment.StageId == stageId.Value);
        }

        if (levelId.HasValue)
        {
            query = query.Where(er => er.StudentEnrollment != null && er.StudentEnrollment.LevelId == levelId.Value);
        }

        if (classId.HasValue)
        {
            query = query.Where(er => er.StudentEnrollment != null && er.StudentEnrollment.ClassId == classId.Value);
        }

        if (examId.HasValue)
        {
            query = query.Where(er => er.ExamSubject != null && er.ExamSubject.ExamSchedule != null && er.ExamSubject.ExamSchedule.Exam != null && er.ExamSubject.ExamSchedule.Exam.Id == examId.Value);
        }

        if (!string.IsNullOrWhiteSpace(currentPublicationStatus))
        {
            var current = currentPublicationStatus.Trim();
            query = query.Where(er =>
                er.PublicationStatus == current ||
                er.PublicationStatus.ToLower() == current.ToLower());
        }

        if (studentIds != null && studentIds.Count > 0)
        {
            query = query.Where(er => studentIds.Contains(er.StudentId));
        }

        var resultsToUpdate = await query.ToListAsync();

        foreach (var result in resultsToUpdate)
        {
            result.PublicationStatus = publicationStatus;
            result.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return resultsToUpdate.Count;
    }

    private string GetStatusFromPercentage(decimal percentage)
    {
        if (percentage >= 80) return "Excellent";
        if (percentage >= 70) return "Very Good";
        if (percentage >= 60) return "Good";
        if (percentage >= 50) return "Pass";
        return "Fail";
    }
}