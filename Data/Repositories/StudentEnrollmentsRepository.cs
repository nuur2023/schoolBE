using Microsoft.EntityFrameworkCore;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Data.Repositories;

public class StudentEnrollmentsRepository(SchoolContext context) : IStudentEnrollments
{
    private readonly SchoolContext _context = context;
    private readonly DbSet<StudentEnrollment> _dbSet = context.Set<StudentEnrollment>();

    public async Task<StudentEnrollment> AddAsync(StudentEnrollment entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<StudentEnrollment> UpdateAsync(StudentEnrollment entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<StudentEnrollment> DeleteAsync(StudentEnrollment entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<StudentEnrollment?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<List<StudentEnrollment>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<(List<StudentEnrollment> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null)
    {
        var query = _dbSet.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(se => 
                se.Name.Contains(search) ||
                se.GenId.Contains(search) ||
                se.Address.Contains(search) ||
                se.Mother.Contains(search));
        }

        var totalCount = await query.CountAsync();
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (data, totalCount);
    }

    public async Task<StudentEnrollment> ActivateAsync(int id)
    {
        var student = await _dbSet.FindAsync(id);
        if (student != null)
        {
            student.IsActiveInApp = !student.IsActiveInApp;
            await _context.SaveChangesAsync();
        }
        return student!;
    }
}
