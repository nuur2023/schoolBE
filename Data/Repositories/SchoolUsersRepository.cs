using Microsoft.EntityFrameworkCore;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Data.Repositories;

public class SchoolUsersRepository(SchoolContext context) : ISchoolUsers
{
    private readonly SchoolContext _context = context;
    private readonly DbSet<SchoolUser> _dbSet = context.Set<SchoolUser>();

    public async Task<SchoolUser> AddAsync(SchoolUser entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<SchoolUser> UpdateAsync(SchoolUser entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<SchoolUser> DeleteAsync(SchoolUser entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<SchoolUser?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<List<SchoolUser>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<(List<SchoolUser> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null)
    {
        var query = _dbSet
            .Include(su => su.User)
            .Include(su => su.School)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(su => 
                (su.User != null && su.User.Name.Contains(search)) ||
                (su.User != null && su.User.Email.Contains(search)) ||
                (su.School != null && su.School.Name.Contains(search)));
        }

        var totalCount = await query.CountAsync();
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (data, totalCount);
    }

    public async Task<SchoolUser> ActivateAsync(int id)
    {
        var schoolUser = await _dbSet.FindAsync(id);
        if (schoolUser != null)
        {
            schoolUser.IsActive = !schoolUser.IsActive;
            await _context.SaveChangesAsync();
        }
        return schoolUser!;
    }

    public async Task<List<SchoolUser>> GetBySchoolIdWithUsersAsync(int schoolId)
    {
        return await _dbSet
            .Where(su => su.SchoolId == schoolId)
            .Include(su => su.User)
            .AsNoTracking()
            .ToListAsync();
    }
}
