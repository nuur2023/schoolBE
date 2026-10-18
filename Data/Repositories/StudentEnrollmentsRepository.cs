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
        return await _dbSet
            .Include(se => se.School)
            .Include(se => se.Stage)
            .Include(se => se.Level)
            .Include(se => se.Class)
            .FirstOrDefaultAsync(se => se.Id == id);
    }

    public async Task<List<StudentEnrollment>> GetAllAsync()
    {
        return await _dbSet
            .Include(se => se.School)
            .Include(se => se.Stage)
            .Include(se => se.Level)
            .Include(se => se.Class)
            .ToListAsync();
    }

    public async Task<(List<StudentEnrollment> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null, bool? isActive = null)
    {
        var query = _dbSet
            .Include(se => se.School)
            .Include(se => se.Stage)
            .Include(se => se.Level)
            .Include(se => se.Class)
            .AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(se => se.IsActive == isActive.Value);
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(se => 
                se.Name.Contains(search) ||
                se.GenId.Contains(search) ||
                se.Address.Contains(search) ||
                se.Mother.Contains(search) ||
                (se.School != null && se.School.Name.Contains(search)) ||
                (se.Stage != null && se.Stage.Name.Contains(search)) ||
                (se.Level != null && se.Level.Name.Contains(search)) ||
                (se.Class != null && se.Class.Name.Contains(search)));
        }

        var totalCount = await query.CountAsync();
        var data = await query
            .OrderByDescending(se => se.CreatedAt)
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

    public async Task<List<StudentEnrollment>> GetByStageLevelClassAsync(int? stageId, int? levelId, int? classId, List<int>? excludeStudentIds = null)
    {
        var query = _dbSet
            .Include(se => se.School)
            .Include(se => se.Stage)
            .Include(se => se.Level)
            .Include(se => se.Class)
            .Where(se => se.IsActive && !se.IsGraduated && !se.IsLeft)
            .AsQueryable();

        if (stageId.HasValue)
        {
            query = query.Where(se => se.StageId == stageId.Value);
        }

        if (levelId.HasValue)
        {
            query = query.Where(se => se.LevelId == levelId.Value);
        }

        if (classId.HasValue)
        {
            query = query.Where(se => se.ClassId == classId.Value);
        }

        if (excludeStudentIds != null && excludeStudentIds.Count > 0)
        {
            query = query.Where(se => !excludeStudentIds.Contains(se.Id));
        }

        return await query
            .OrderBy(se => se.Name)
            .ToListAsync();
    }
}