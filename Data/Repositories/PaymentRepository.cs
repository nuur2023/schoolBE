using Microsoft.EntityFrameworkCore;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Data.Repositories;

public class PaymentRepository(SchoolContext context) : IPayment
{
    private readonly SchoolContext _context = context;
    private readonly DbSet<Payment> _dbSet = context.Set<Payment>();

    public async Task<Payment> AddAsync(Payment entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Payment> UpdateAsync(Payment entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Payment> DeleteAsync(Payment entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Payment?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<List<Payment>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<(List<Payment> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null)
    {
        var query = _dbSet.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(p => 
                p.ReferenceNo.Contains(search) ||
                p.Type.Contains(search) ||
                (p.ReferenceId != null && p.ReferenceId.Contains(search)));
        }

        var totalCount = await query.CountAsync();
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (data, totalCount);
    }
}
