using Microsoft.EntityFrameworkCore;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Data.Repositories;

public class ClassesRepository(SchoolContext context) : IClasses
{
    private readonly SchoolContext _context = context;
    private readonly DbSet<Class> _dbSet = context.Set<Class>();

    public async Task<Class> AddAsync(Class entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Class> UpdateAsync(Class entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Class> DeleteAsync(Class entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Class?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<List<Class>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<(List<Class> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null)
    {
        var query = _dbSet
            .Include(c => c.Level)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(c => 
                c.Name.Contains(search) ||
                (c.Level != null && c.Level.Name.Contains(search)));
        }

        var totalCount = await query.CountAsync();
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (data, totalCount);
    }

    public async Task<Class> ActivateAsync(int id)
    {
        var classEntity = await _dbSet.FindAsync(id);
        if (classEntity != null)
        {
            classEntity.IsActive = !classEntity.IsActive;
            await _context.SaveChangesAsync();
        }
        return classEntity!;
    }
}
