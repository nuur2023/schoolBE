using Microsoft.EntityFrameworkCore;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Data.Repositories;

public class StagesRepository(SchoolContext context) : IStages
{
    private readonly SchoolContext _context = context;
    private readonly DbSet<Stage> _dbSet = context.Set<Stage>();

    public async Task<Stage> AddAsync(Stage entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Stage> UpdateAsync(Stage entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Stage> DeleteAsync(Stage entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Stage?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<List<Stage>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<(List<Stage> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null)
    {
        var query = _dbSet.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(s => s.Name.Contains(search));
        }

        var totalCount = await query.CountAsync();
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (data, totalCount);
    }

    public async Task<Stage> ActivateAsync(int id)
    {
        var stage = await _dbSet.FindAsync(id);
        if (stage != null)
        {
            stage.IsActive = !stage.IsActive;
            await _context.SaveChangesAsync();
        }
        return stage!;
    }
}
