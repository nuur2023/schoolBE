using Microsoft.EntityFrameworkCore;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using System.Linq.Expressions;

namespace schoolBE.Data.Repositories;

public class BaseRepository<T> : IBaseSchoolInterface<T> where T : class
{
    protected readonly SchoolContext _context;
    protected readonly DbSet<T> _dbSet;

    public BaseRepository(SchoolContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task<T> UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task<T> DeleteAsync(T entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public virtual async Task<List<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public virtual async Task<(List<T> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null)
    {
        var query = _dbSet.AsQueryable();

        // Apply search if provided
        if (!string.IsNullOrEmpty(search))
        {
            query = ApplySearch(query, search);
        }

        var totalCount = await query.CountAsync();
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (data, totalCount);
    }

    protected virtual IQueryable<T> ApplySearch(IQueryable<T> query, string search)
    {
        // Default implementation - override in derived classes for specific search logic
        return query;
    }
}

