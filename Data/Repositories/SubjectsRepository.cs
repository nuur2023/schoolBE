using Microsoft.EntityFrameworkCore;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Data.Repositories;

public class SubjectsRepository(SchoolContext context) : ISubjects
{
    private readonly SchoolContext _context = context;
    private readonly DbSet<Subject> _dbSet = context.Set<Subject>();

    public async Task<Subject> AddAsync(Subject entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Subject> UpdateAsync(Subject entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Subject> DeleteAsync(Subject entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Subject?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<List<Subject>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<(List<Subject> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null)
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

    public async Task<Subject> ActivateAsync(int id)
    {
        var subject = await _dbSet.FindAsync(id);
        if (subject != null)
        {
            subject.IsActive = !subject.IsActive;
            await _context.SaveChangesAsync();
        }
        return subject!;
    }
}
