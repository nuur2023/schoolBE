using Microsoft.EntityFrameworkCore;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Data.Repositories;

public class StudentGraduationRepository(SchoolContext context) : IStudentGraduation
{
    private readonly SchoolContext _context = context;
    private readonly DbSet<StudentGraduation> _dbSet = context.Set<StudentGraduation>();

    public async Task<StudentGraduation> AddAsync(StudentGraduation entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<StudentGraduation> UpdateAsync(StudentGraduation entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<StudentGraduation> DeleteAsync(StudentGraduation entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<StudentGraduation?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(sg => sg.Student)
                .ThenInclude(s => s!.School)
            .Include(sg => sg.Student)
                .ThenInclude(s => s!.Stage)
            .Include(sg => sg.Student)
                .ThenInclude(s => s!.Level)
            .Include(sg => sg.Student)
                .ThenInclude(s => s!.Class)
            .Include(sg => sg.School)
            .Include(sg => sg.Creator)
            .FirstOrDefaultAsync(sg => sg.Id == id);
    }

    public async Task<List<StudentGraduation>> GetAllAsync()
    {
        return await _dbSet
            .Include(sg => sg.Student)
                .ThenInclude(s => s!.School)
            .Include(sg => sg.Student)
                .ThenInclude(s => s!.Stage)
            .Include(sg => sg.Student)
                .ThenInclude(s => s!.Level)
            .Include(sg => sg.Student)
                .ThenInclude(s => s!.Class)
            .Include(sg => sg.School)
            .Include(sg => sg.Creator)
            .OrderByDescending(sg => sg.GraduationDate)
            .ToListAsync();
    }

    public async Task<(List<StudentGraduation> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null, int? schoolId = null, int? studentId = null)
    {
        var query = _dbSet
            .Include(sg => sg.Student)
                .ThenInclude(s => s!.School)
            .Include(sg => sg.Student)
                .ThenInclude(s => s!.Stage)
            .Include(sg => sg.Student)
                .ThenInclude(s => s!.Level)
            .Include(sg => sg.Student)
                .ThenInclude(s => s!.Class)
            .Include(sg => sg.School)
            .Include(sg => sg.Creator)
            .AsQueryable();

        if (schoolId.HasValue)
        {
            query = query.Where(sg => sg.SchoolId == schoolId.Value || 
                (sg.Student != null && sg.Student.SchoolId == schoolId.Value));
        }

        if (studentId.HasValue)
        {
            query = query.Where(sg => sg.StudentId == studentId.Value);
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(sg =>
                (sg.Student != null && sg.Student.Name != null && EF.Functions.Like(sg.Student.Name, $"%{search}%")) ||
                (sg.Student != null && sg.Student.GenId != null && EF.Functions.Like(sg.Student.GenId, $"%{search}%")) ||
                (sg.Student != null && sg.Student.Address != null && EF.Functions.Like(sg.Student.Address, $"%{search}%")) ||
                (sg.Student != null && sg.Student.Mother != null && EF.Functions.Like(sg.Student.Mother, $"%{search}%")) ||
                (sg.Reason != null && EF.Functions.Like(sg.Reason, $"%{search}%")));
        }

        var totalCount = await query.CountAsync();
        var data = await query
            .OrderByDescending(sg => sg.GraduationDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (data, totalCount);
    }
}







