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
        return await _dbSet.FindAsync(id);
    }

    public async Task<List<ExamSchedule>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<(List<ExamSchedule> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null)
    {
        var query = _dbSet.AsQueryable();

        var totalCount = await query.CountAsync();
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (data, totalCount);
    }
}
