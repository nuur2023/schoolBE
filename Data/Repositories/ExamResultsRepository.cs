using Microsoft.EntityFrameworkCore;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

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
            .Include(er => er.ExamSchedule)
                .ThenInclude(es => es!.Exam)
            .Include(er => er.ExamSchedule)
                .ThenInclude(es => es!.LevelSubject)
                    .ThenInclude(ls => ls!.Level)
            .Include(er => er.ExamSchedule)
                .ThenInclude(es => es!.LevelSubject)
                    .ThenInclude(ls => ls!.Subject)
            .Include(er => er.StudentEnrollment)
                .ThenInclude(se => se!.School)
            .Include(er => er.StudentEnrollment)
                .ThenInclude(se => se!.Level)
            .Include(er => er.StudentEnrollment)
                .ThenInclude(se => se!.Class)
            .FirstOrDefaultAsync(er => er.Id == id);
    }

    public async Task<List<ExamResult>> GetAllAsync()
    {
        return await _dbSet
            .Include(er => er.ExamSchedule)
                .ThenInclude(es => es!.Exam)
            .Include(er => er.ExamSchedule)
                .ThenInclude(es => es!.LevelSubject)
                    .ThenInclude(ls => ls!.Level)
            .Include(er => er.ExamSchedule)
                .ThenInclude(es => es!.LevelSubject)
                    .ThenInclude(ls => ls!.Subject)
            .Include(er => er.StudentEnrollment)
                .ThenInclude(se => se!.School)
            .Include(er => er.StudentEnrollment)
                .ThenInclude(se => se!.Level)
            .Include(er => er.StudentEnrollment)
                .ThenInclude(se => se!.Class)
            .ToListAsync();
    }

    public async Task<(List<ExamResult> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null)
    {
        var query = _dbSet
            .Include(er => er.ExamSchedule)
                .ThenInclude(es => es!.Exam)
            .Include(er => er.ExamSchedule)
                .ThenInclude(es => es!.LevelSubject)
                    .ThenInclude(ls => ls!.Level)
            .Include(er => er.ExamSchedule)
                .ThenInclude(es => es!.LevelSubject)
                    .ThenInclude(ls => ls!.Subject)
            .Include(er => er.StudentEnrollment)
                .ThenInclude(se => se!.School)
            .Include(er => er.StudentEnrollment)
                .ThenInclude(se => se!.Level)
            .Include(er => er.StudentEnrollment)
                .ThenInclude(se => se!.Class)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(er =>
                (er.StudentEnrollment != null && er.StudentEnrollment.Name != null && er.StudentEnrollment.Name.ToLower().Contains(searchLower)) ||
                (er.StudentEnrollment != null && er.StudentEnrollment.GenId != null && er.StudentEnrollment.GenId.ToLower().Contains(searchLower)) ||
                (er.ExamSchedule != null && er.ExamSchedule.Exam != null && er.ExamSchedule.Exam.Title != null && er.ExamSchedule.Exam.Title.ToLower().Contains(searchLower)) ||
                (er.ExamSchedule != null && er.ExamSchedule.LevelSubject != null && er.ExamSchedule.LevelSubject.Subject != null && er.ExamSchedule.LevelSubject.Subject.Name != null && er.ExamSchedule.LevelSubject.Subject.Name.ToLower().Contains(searchLower)) ||
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
}