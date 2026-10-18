using Microsoft.EntityFrameworkCore;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Data.Repositories;

public class ExamRoomsRepository(SchoolContext context) : IExamRooms
{
    private readonly SchoolContext _context = context;
    private readonly DbSet<ExamRoom> _dbSet = context.Set<ExamRoom>();

    public async Task<ExamRoom> AddAsync(ExamRoom entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<ExamRoom> UpdateAsync(ExamRoom entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<ExamRoom> DeleteAsync(ExamRoom entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<ExamRoom?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(er => er.School)
            .FirstOrDefaultAsync(er => er.Id == id);
    }

    public async Task<List<ExamRoom>> GetAllAsync()
    {
        return await _dbSet
            .Include(er => er.School)
            .OrderBy(er => er.Name)
            .ToListAsync();
    }

    public async Task<(List<ExamRoom> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null)
    {
        var query = _dbSet
            .Include(er => er.School)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(er =>
                er.Name.Contains(search) ||
                (er.School != null && er.School.Name.Contains(search)));
        }

        var totalCount = await query.CountAsync();
        var data = await query
            .OrderBy(er => er.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (data, totalCount);
    }
}













