using Microsoft.EntityFrameworkCore;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Data.Repositories;

public class PersonsRepository(SchoolContext context) : IPersons
{
    private readonly SchoolContext _context = context;
    private readonly DbSet<Person> _dbSet = context.Set<Person>();

    public async Task<Person> AddAsync(Person entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Person> UpdateAsync(Person entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Person> DeleteAsync(Person entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Person?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(p => p.Role)
            .Include(p => p.School)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<Person>> GetAllAsync()
    {
        return await _dbSet
            .Include(p => p.Role)
            .Include(p => p.School)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<(List<Person> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null)
    {
        var query = _dbSet
            .Include(p => p.Role)
            .Include(p => p.School)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(p =>
                p.Name.Contains(search) ||
                (p.Email != null && p.Email.Contains(search)) ||
                (p.Address != null && p.Address.Contains(search)) ||
                (p.Role != null && p.Role.Name.Contains(search)) ||
                (p.School != null && p.School.Name.Contains(search)));
        }

        var totalCount = await query.CountAsync();
        var data = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (data, totalCount);
    }
}













