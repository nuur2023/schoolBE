using Microsoft.EntityFrameworkCore;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Data.Repositories;

public class StudentTransferRepository(SchoolContext context) : IStudentTransfer
{
    private readonly SchoolContext _context = context;
    private readonly DbSet<StudentTransfer> _dbSet = context.Set<StudentTransfer>();

    public async Task<StudentTransfer> AddAsync(StudentTransfer entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<StudentTransfer> UpdateAsync(StudentTransfer entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<StudentTransfer> DeleteAsync(StudentTransfer entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<StudentTransfer?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(st => st.Student)
                .ThenInclude(s => s!.School)
            .Include(st => st.Student)
                .ThenInclude(s => s!.Stage)
            .Include(st => st.Student)
                .ThenInclude(s => s!.Level)
            .Include(st => st.Student)
                .ThenInclude(s => s!.Class)
            .Include(st => st.School)
            .Include(st => st.Creator)
            .Include(st => st.FromStage)
            .Include(st => st.FromLevel)
            .Include(st => st.FromClass)
            .Include(st => st.ToStage)
            .Include(st => st.ToLevel)
            .Include(st => st.ToClass)
            .FirstOrDefaultAsync(st => st.Id == id);
    }

    public async Task<List<StudentTransfer>> GetAllAsync()
    {
        return await _dbSet
            .Include(st => st.Student)
                .ThenInclude(s => s!.School)
            .Include(st => st.Student)
                .ThenInclude(s => s!.Stage)
            .Include(st => st.Student)
                .ThenInclude(s => s!.Level)
            .Include(st => st.Student)
                .ThenInclude(s => s!.Class)
            .Include(st => st.School)
            .Include(st => st.Creator)
            .Include(st => st.FromStage)
            .Include(st => st.FromLevel)
            .Include(st => st.FromClass)
            .Include(st => st.ToStage)
            .Include(st => st.ToLevel)
            .Include(st => st.ToClass)
            .OrderByDescending(st => st.TransferDate)
            .ToListAsync();
    }

    public async Task<(List<StudentTransfer> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null, int? schoolId = null, int? studentId = null)
    {
        var query = _dbSet
            .Include(st => st.Student)
                .ThenInclude(s => s!.School)
            .Include(st => st.Student)
                .ThenInclude(s => s!.Stage)
            .Include(st => st.Student)
                .ThenInclude(s => s!.Level)
            .Include(st => st.Student)
                .ThenInclude(s => s!.Class)
            .Include(st => st.School)
            .Include(st => st.Creator)
            .Include(st => st.FromStage)
            .Include(st => st.FromLevel)
            .Include(st => st.FromClass)
            .Include(st => st.ToStage)
            .Include(st => st.ToLevel)
            .Include(st => st.ToClass)
            .AsQueryable();

        if (schoolId.HasValue)
        {
            query = query.Where(st => st.SchoolId == schoolId.Value || 
                (st.Student != null && st.Student.SchoolId == schoolId.Value));
        }

        if (studentId.HasValue)
        {
            query = query.Where(st => st.StudentId == studentId.Value);
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(st =>
                (st.Student != null && st.Student.Name != null && EF.Functions.Like(st.Student.Name, $"%{search}%")) ||
                (st.Student != null && st.Student.GenId != null && EF.Functions.Like(st.Student.GenId, $"%{search}%")) ||
                (st.Reason != null && EF.Functions.Like(st.Reason, $"%{search}%")));
        }

        var totalCount = await query.CountAsync();
        var data = await query
            .OrderByDescending(st => st.TransferDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (data, totalCount);
    }

    public async Task<List<StudentTransfer>> GetByStudentIdAsync(int studentId)
    {
        return await _dbSet
            .Include(st => st.Student)
                .ThenInclude(s => s!.School)
            .Include(st => st.Student)
                .ThenInclude(s => s!.Stage)
            .Include(st => st.Student)
                .ThenInclude(s => s!.Level)
            .Include(st => st.Student)
                .ThenInclude(s => s!.Class)
            .Include(st => st.School)
            .Include(st => st.Creator)
            .Include(st => st.FromStage)
            .Include(st => st.FromLevel)
            .Include(st => st.FromClass)
            .Include(st => st.ToStage)
            .Include(st => st.ToLevel)
            .Include(st => st.ToClass)
            .Where(st => st.StudentId == studentId)
            .OrderByDescending(st => st.TransferDate)
            .ToListAsync();
    }
}




















