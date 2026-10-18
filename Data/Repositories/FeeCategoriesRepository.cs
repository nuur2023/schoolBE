using Microsoft.EntityFrameworkCore;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Data.Repositories;

public class FeeCategoriesRepository(SchoolContext context) : IFeeCategories
{
    private readonly SchoolContext _context = context;
    private readonly DbSet<FeeCategories> _dbSet = context.Set<FeeCategories>();

    public async Task<FeeCategories> AddAsync(FeeCategories entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<FeeCategories> UpdateAsync(FeeCategories entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<FeeCategories> DeleteAsync(FeeCategories entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<FeeCategories?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<List<FeeCategories>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<(List<FeeCategories> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null)
    {
        var query = _dbSet.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(fc => 
                (fc.Name != null && fc.Name.Contains(search)) || 
                (fc.Type != null && fc.Type.Contains(search)));
        }

        var totalCount = await query.CountAsync();
        var data = await query
            .OrderBy(fc => fc.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (data, totalCount);
    }
}

















