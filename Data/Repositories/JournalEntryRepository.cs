using Microsoft.EntityFrameworkCore;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Data.Repositories;

public class JournalEntryRepository(SchoolContext context) : IJournalEntry
{
    private readonly SchoolContext _context = context;
    private readonly DbSet<JournalEntry> _dbSet = context.Set<JournalEntry>();

    public async Task<JournalEntry> AddAsync(JournalEntry entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<JournalEntry> UpdateAsync(JournalEntry entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<JournalEntry> DeleteAsync(JournalEntry entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<JournalEntry?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<List<JournalEntry>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<(List<JournalEntry> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null)
    {
        var query = _dbSet.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(je => 
                je.ReferenceNo.Contains(search) ||
                je.Status.Contains(search) ||
                (je.Description != null && je.Description.Contains(search)));
        }

        var totalCount = await query.CountAsync();
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (data, totalCount);
    }
}
