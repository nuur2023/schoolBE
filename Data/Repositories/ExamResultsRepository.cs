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
        return await _dbSet.FindAsync(id);
    }

    public async Task<List<ExamResult>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<(List<ExamResult> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null)
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
