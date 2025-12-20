using Microsoft.EntityFrameworkCore;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Data.Repositories;

public class ChartOfAccountRepository(SchoolContext context) : IChartOfAccount
{
    private readonly SchoolContext _context = context;
    private readonly DbSet<ChartOfAccount> _dbSet = context.Set<ChartOfAccount>();

    public async Task<ChartOfAccount> AddAsync(ChartOfAccount entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<ChartOfAccount> UpdateAsync(ChartOfAccount entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<ChartOfAccount> DeleteAsync(ChartOfAccount entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<ChartOfAccount?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<List<ChartOfAccount>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<(List<ChartOfAccount> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null)
    {
        var query = _dbSet.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(coa => 
                coa.Name.Contains(search) ||
                coa.Code.Contains(search) ||
                coa.NormalBalance.Contains(search));
        }

        var totalCount = await query.CountAsync();
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (data, totalCount);
    }
}
