using Microsoft.EntityFrameworkCore;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Data.Repositories;

public class YearEndClosingEntryRepository(SchoolContext context) : IYearEndClosingEntry
{
    private readonly SchoolContext _context = context;
    private readonly DbSet<YearEndClosingEntry> _dbSet = context.Set<YearEndClosingEntry>();

    public async Task<YearEndClosingEntry> AddAsync(YearEndClosingEntry entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<YearEndClosingEntry> UpdateAsync(YearEndClosingEntry entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<YearEndClosingEntry> DeleteAsync(YearEndClosingEntry entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<YearEndClosingEntry?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<List<YearEndClosingEntry>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<(List<YearEndClosingEntry> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null)
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
