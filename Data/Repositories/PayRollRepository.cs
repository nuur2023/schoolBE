using Microsoft.EntityFrameworkCore;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Data.Repositories;

public class PayRollRepository(SchoolContext context) : IPayRoll
{
    private readonly SchoolContext _context = context;
    private readonly DbSet<PayRoll> _dbSet = context.Set<PayRoll>();

    public async Task<PayRoll> AddAsync(PayRoll entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<PayRoll> UpdateAsync(PayRoll entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<PayRoll> DeleteAsync(PayRoll entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<PayRoll?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<List<PayRoll>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<(List<PayRoll> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null)
    {
        var query = _dbSet.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(pr => pr.PayPeriod.Contains(search));
        }

        var totalCount = await query.CountAsync();
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (data, totalCount);
    }
}
