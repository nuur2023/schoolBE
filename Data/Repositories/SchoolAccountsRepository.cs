using Microsoft.EntityFrameworkCore;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Data.Repositories;

public class SchoolAccountsRepository(SchoolContext context) : ISchoolAccounts
{
    private readonly SchoolContext _context = context;
    private readonly DbSet<SchoolAccount> _dbSet = context.Set<SchoolAccount>();

    public async Task<SchoolAccount> AddAsync(SchoolAccount entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<SchoolAccount> UpdateAsync(SchoolAccount entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<SchoolAccount> DeleteAsync(SchoolAccount entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<SchoolAccount?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<List<SchoolAccount>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<(List<SchoolAccount> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null)
    {
        var query = _dbSet.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(sa => 
                sa.Name.Contains(search) ||
                sa.AccountNumber.Contains(search) ||
                sa.SchoolAccountType.Contains(search));
        }

        var totalCount = await query.CountAsync();
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (data, totalCount);
    }
}
