using Microsoft.EntityFrameworkCore;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Data.Repositories;

public class ExamScheduleRepository(SchoolContext context) : IExamSchedule
{
    private readonly SchoolContext _context = context;
    private readonly DbSet<ExamSchedule> _dbSet = context.Set<ExamSchedule>();

    public async Task<ExamSchedule> AddAsync(ExamSchedule entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<ExamSchedule> UpdateAsync(ExamSchedule entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<ExamSchedule> DeleteAsync(ExamSchedule entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<ExamSchedule?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(es => es.Exam)
            .Include(es => es.LevelSubject)
                .ThenInclude(ls => ls!.Level)
            .Include(es => es.LevelSubject)
                .ThenInclude(ls => ls!.Subject)
            .FirstOrDefaultAsync(es => es.Id == id);
    }

    public async Task<List<ExamSchedule>> GetAllAsync()
    {
        return await _dbSet
            .Include(es => es.Exam)
            .Include(es => es.LevelSubject)
                .ThenInclude(ls => ls!.Level)
            .Include(es => es.LevelSubject)
                .ThenInclude(ls => ls!.Subject)
            .ToListAsync();
    }

    public async Task<(List<ExamSchedule> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null)
    {
        var query = _dbSet
            .Include(es => es.Exam)
            .Include(es => es.LevelSubject)
                .ThenInclude(ls => ls!.Level)
            .Include(es => es.LevelSubject)
                .ThenInclude(ls => ls!.Subject)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(es =>
                (es.Exam != null && es.Exam.Title.Contains(search)) ||
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
