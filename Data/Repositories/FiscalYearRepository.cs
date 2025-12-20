using Microsoft.EntityFrameworkCore;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Data.Repositories;

public class FiscalYearRepository(SchoolContext context) : IFiscalYear
{
    private readonly SchoolContext _context = context;
    private readonly DbSet<FiscalYear> _dbSet = context.Set<FiscalYear>();

    public async Task<FiscalYear> AddAsync(FiscalYear entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<FiscalYear> UpdateAsync(FiscalYear entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<FiscalYear> DeleteAsync(FiscalYear entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<FiscalYear?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<List<FiscalYear>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<(List<FiscalYear> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null)
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
