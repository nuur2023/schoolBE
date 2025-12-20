using Microsoft.EntityFrameworkCore;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Data.Repositories;

public class LevelsRepository(SchoolContext context) : ILevels
{
    private readonly SchoolContext _context = context;
    private readonly DbSet<Level> _dbSet = context.Set<Level>();

    public async Task<Level> AddAsync(Level entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Level> UpdateAsync(Level entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Level> DeleteAsync(Level entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Level?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<List<Level>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<(List<Level> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null)
    {
        var query = _dbSet
            .Include(l => l.Stage)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(l => 
                l.Name.Contains(search) ||
                (l.Stage != null && l.Stage.Name.Contains(search)));
        }

        var totalCount = await query.CountAsync();
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (data, totalCount);
    }

    public async Task<Level> ActivateAsync(int id)
    {
        var level = await _dbSet.FindAsync(id);
        if (level != null)
        {
            level.IsActive = !level.IsActive;
            await _context.SaveChangesAsync();
        }
        return level!;
    }
}
