using Microsoft.EntityFrameworkCore;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Data.Repositories;

public class SchoolsRepository(SchoolContext context) : ISchools
{
    private readonly SchoolContext _context = context;
    private readonly DbSet<School> _dbSet = context.Set<School>();

    public async Task<School> AddAsync(School entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<School> UpdateAsync(School entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<School> DeleteAsync(School entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<School?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<List<School>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<(List<School> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null)
    {
        var query = _dbSet.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(s => 
                s.Name.Contains(search) || 
                s.Address.Contains(search));
        }

        var totalCount = await query.CountAsync();
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (data, totalCount);
    }

    public async Task<School> ActivateAsync(int id)
    {
        var school = await _dbSet.FindAsync(id);
        if (school != null)
        {
            school.IsActive = !school.IsActive;
            await _context.SaveChangesAsync();
        }
        return school!;
    }
}
