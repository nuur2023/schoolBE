using Microsoft.EntityFrameworkCore;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Data.Repositories;

public class ClassSubjectsRepository(SchoolContext context) : IClassSubjects
{
    private readonly SchoolContext _context = context;
    private readonly DbSet<ClassSubject> _dbSet = context.Set<ClassSubject>();

    public async Task<ClassSubject> AddAsync(ClassSubject entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<ClassSubject> UpdateAsync(ClassSubject entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<ClassSubject> DeleteAsync(ClassSubject entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<ClassSubject?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(cs => cs.Class!)
                .ThenInclude(c => c.Level!)
                .ThenInclude(l => l!.Stage)
            .Include(cs => cs.Subject)
            .Include(cs => cs.Person)
            .FirstOrDefaultAsync(cs => cs.Id == id);
    }

    public async Task<List<ClassSubject>> GetAllAsync()
    {
        return await _dbSet
            .Include(cs => cs.Class)
            .Include(cs => cs.Subject)
            .Include(cs => cs.Person)
            .ToListAsync();
    }

    public async Task<(List<ClassSubject> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null)
    {
        var query = _dbSet
            .Include(cs => cs.Class!)
                .ThenInclude(c => c.Level!)
                .ThenInclude(l => l!.Stage)
            .Include(cs => cs.Subject)
            .Include(cs => cs.Person)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(cs => 
                (cs.Class != null && cs.Class.Name.Contains(search)) ||
                (cs.Subject != null && cs.Subject.Name.Contains(search)) ||
                (cs.Person != null && cs.Person.Name.Contains(search)));
        }

        var totalCount = await query.CountAsync();
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (data, totalCount);
    }

    public async Task<List<Subject>> GetSubjectsByClassIdAsync(int classId)
    {
        return await _dbSet
            .Include(cs => cs.Subject)
            .Where(cs => cs.ClassId == classId && cs.Subject != null && cs.Subject.IsActive)
            .Select(cs => cs.Subject!)
            .Distinct()
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

}