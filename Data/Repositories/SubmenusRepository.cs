using Microsoft.EntityFrameworkCore;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Data.Repositories;

public class SubmenusRepository(SchoolContext context) : ISubmenus
{
    private readonly SchoolContext _context = context;
    private readonly DbSet<Submenu> _dbSet = context.Set<Submenu>();

    public async Task<Submenu> AddAsync(Submenu entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Submenu> UpdateAsync(Submenu entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Submenu> DeleteAsync(Submenu entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Submenu?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<List<Submenu>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<(List<Submenu> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null)
    {
        var query = _dbSet
            .Include(s => s.Menu)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(s => 
                s.Name.Contains(search) || 
                s.Route.Contains(search) ||
                (s.Menu != null && s.Menu.Name.Contains(search)));
        }

        var totalCount = await query.CountAsync();
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (data, totalCount);
    }
}
