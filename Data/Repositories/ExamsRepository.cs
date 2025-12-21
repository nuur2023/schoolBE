using Microsoft.EntityFrameworkCore;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Data.Repositories;

public class ExamsRepository(SchoolContext context) : IExams
{
    private readonly SchoolContext _context = context;
    private readonly DbSet<Exam> _dbSet = context.Set<Exam>();

    public async Task<Exam> AddAsync(Exam entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Exam> UpdateAsync(Exam entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Exam> DeleteAsync(Exam entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Exam?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(e => e.School)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<List<Exam>> GetAllAsync()
    {
        return await _dbSet
            .Include(e => e.School)
            .ToListAsync();
    }

    public async Task<(List<Exam> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null)
    {
        var query = _dbSet
            .Include(e => e.School)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(e => 
                e.Title.Contains(search) ||
                (e.School != null && e.School.Name.Contains(search)));
        }

        var totalCount = await query.CountAsync();
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (data, totalCount);
    }

    public async Task<Exam> ActivateAsync(int id)
    {
        var exam = await _dbSet.FindAsync(id);
        if (exam != null)
        {
            exam.IsActive = !exam.IsActive;
            await _context.SaveChangesAsync();
        }
        return exam!;
    }
}
