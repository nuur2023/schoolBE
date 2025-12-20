using Microsoft.EntityFrameworkCore;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using schoolBE.Models;
using schoolBE.ViewModels;

namespace schoolBE.Data.Repositories;

public class RolesRepository(SchoolContext context) : IRoles
{
    private readonly SchoolContext _context = context;
    private readonly DbSet<Role> _dbSet = context.Set<Role>();

    public async Task<Role> AddAsync(Role entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Role> UpdateAsync(Role entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Role> DeleteAsync(Role entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Role?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<List<Role>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<(List<Role> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null)
    {
        var query = _dbSet.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(r => r.Name.Contains(search));
        }

        var totalCount = await query.CountAsync();
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (data, totalCount);
    }

    public async Task<List<RoleVM>> GetRolesAsync()
    {
        var roles = await _dbSet.ToListAsync();
        return roles.Select(r => new RoleVM
        {
            Id = r.Id,
            Name = r.Name,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        }).ToList();
    }
}
