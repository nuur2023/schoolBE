using Microsoft.EntityFrameworkCore;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Data.Repositories;

public class ExamSubjectRepository(SchoolContext context) : IExamSubject
{
    private readonly SchoolContext _context = context;
    private readonly DbSet<ExamSubject> _dbSet = context.Set<ExamSubject>();

    public async Task<ExamSubject> AddAsync(ExamSubject entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<ExamSubject> UpdateAsync(ExamSubject entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<ExamSubject> DeleteAsync(ExamSubject entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<ExamSubject?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(es => es.ExamSchedule)
                .ThenInclude(s => s!.Exam)
            .Include(es => es.LevelSubject)
                .ThenInclude(ls => ls!.Level)
            .Include(es => es.LevelSubject)
                .ThenInclude(ls => ls!.Subject)
            .FirstOrDefaultAsync(es => es.Id == id);
    }

    public async Task<List<ExamSubject>> GetAllAsync()
    {
        return await _dbSet
            .Include(es => es.ExamSchedule)
                .ThenInclude(s => s!.Exam)
            .Include(es => es.LevelSubject)
                .ThenInclude(ls => ls!.Level)
            .Include(es => es.LevelSubject)
                .ThenInclude(ls => ls!.Subject)
            .ToListAsync();
    }

    public async Task<(List<ExamSubject> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null, int? examScheduleId = null, DateTime? examDate = null)
    {
        var query = _dbSet
            .Include(es => es.ExamSchedule)
                .ThenInclude(s => s!.Exam)
            .Include(es => es.LevelSubject)
                .ThenInclude(ls => ls!.Level)
            .Include(es => es.LevelSubject)
                .ThenInclude(ls => ls!.Subject)
            .AsQueryable();

        if (examScheduleId.HasValue)
        {
            query = query.Where(es => es.ExamScheduleId == examScheduleId.Value);
        }

        if (examDate.HasValue)
        {
            var d = examDate.Value.Date;
            query = query.Where(es => es.ExamDate.Date == d);
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(es =>
                (es.ExamSchedule != null && es.ExamSchedule.Exam != null && es.ExamSchedule.Exam.Title.Contains(search)) ||
                (es.LevelSubject != null && es.LevelSubject.Level != null && es.LevelSubject.Level.Name.Contains(search)) ||
                (es.LevelSubject != null && es.LevelSubject.Subject != null && es.LevelSubject.Subject.Name.Contains(search)));
        }

        var totalCount = await query.CountAsync();
        var data = await query
            .OrderBy(es => es.ExamDate)
            .ThenBy(es => es.StartTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (data, totalCount);
    }
}


























