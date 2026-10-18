using Microsoft.EntityFrameworkCore;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Data.Repositories;

public class LevelSubjectsRepository(SchoolContext context) : ILevelSubjects
{
    private readonly SchoolContext _context = context;
    private readonly DbSet<LevelSubject> _dbSet = context.Set<LevelSubject>();

    public async Task<LevelSubject> AddAsync(LevelSubject entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<LevelSubject> UpdateAsync(LevelSubject entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<LevelSubject> DeleteAsync(LevelSubject entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<LevelSubject?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(ls => ls.Level)
            .Include(ls => ls.Subject)
            .FirstOrDefaultAsync(ls => ls.Id == id);
    }

    public async Task<List<LevelSubject>> GetAllAsync()
    {
        return await _dbSet
            .Include(ls => ls.Level)
            .Include(ls => ls.Subject)
            .ToListAsync();
    }

    public async Task<(List<LevelSubject> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null)
    {
        var query = _dbSet
            .Include(ls => ls.Level)
            .Include(ls => ls.Subject)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(ls => 
                (ls.Level != null && ls.Level.Name.Contains(search)) ||
                (ls.Subject != null && ls.Subject.Name.Contains(search)));
        }

        var totalCount = await query.CountAsync();
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (data, totalCount);
    }

    public async Task<List<Subject>> GetSubjectsByLevelIdAsync(int levelId)
    {
        return await _dbSet
            .Include(ls => ls.Subject)
            .Where(ls => ls.LevelId == levelId && ls.Subject != null && ls.Subject.IsActive)
            .Select(ls => ls.Subject!)
            .Distinct()
            .OrderBy(s => s.Name)
            .ToListAsync();
    }
}