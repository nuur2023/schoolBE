using Microsoft.EntityFrameworkCore;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Data.Repositories;

public class StudentExitRepository(SchoolContext context) : IStudentExit
{
    private readonly SchoolContext _context = context;
    private readonly DbSet<StudentExit> _dbSet = context.Set<StudentExit>();

    public async Task<StudentExit> AddAsync(StudentExit entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<StudentExit> UpdateAsync(StudentExit entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<StudentExit> DeleteAsync(StudentExit entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<StudentExit?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(se => se.Student)
                .ThenInclude(s => s!.School)
            .Include(se => se.Student)
                .ThenInclude(s => s!.Stage)
            .Include(se => se.Student)
                .ThenInclude(s => s!.Level)
            .Include(se => se.Student)
                .ThenInclude(s => s!.Class)
            .Include(se => se.School)
            .Include(se => se.Creator)
            .FirstOrDefaultAsync(se => se.Id == id);
    }

    public async Task<List<StudentExit>> GetAllAsync()
    {
        return await _dbSet
            .Include(se => se.Student)
                .ThenInclude(s => s!.School)
            .Include(se => se.Student)
                .ThenInclude(s => s!.Stage)
            .Include(se => se.Student)
                .ThenInclude(s => s!.Level)
            .Include(se => se.Student)
                .ThenInclude(s => s!.Class)
            .Include(se => se.School)
            .Include(se => se.Creator)
            .OrderByDescending(se => se.ExitDate)
            .ToListAsync();
    }

    public async Task<(List<StudentExit> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null, int? schoolId = null, int? studentId = null)
    {
        var query = _dbSet
            .Include(se => se.Student)
                .ThenInclude(s => s!.School)
            .Include(se => se.Student)
                .ThenInclude(s => s!.Stage)
            .Include(se => se.Student)
                .ThenInclude(s => s!.Level)
            .Include(se => se.Student)
                .ThenInclude(s => s!.Class)
            .Include(se => se.School)
            .Include(se => se.Creator)
            .AsQueryable();

        if (schoolId.HasValue)
        {
            query = query.Where(se => se.SchoolId == schoolId.Value);
        }

        if (studentId.HasValue)
        {
            query = query.Where(se => se.StudentId == studentId.Value);
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(se =>
                (se.Student != null && se.Student.Name.Contains(search)) ||
                (se.Student != null && se.Student.GenId.Contains(search)) ||
                (se.Reason != null && se.Reason.Contains(search)));
        }

        var totalCount = await query.CountAsync();
        var data = await query
            .OrderByDescending(se => se.ExitDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (data, totalCount);
    }
}

