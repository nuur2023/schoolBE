using Microsoft.EntityFrameworkCore;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Data.Repositories;

public class PaymentMethodsRepository(SchoolContext context) : IPaymentMethods
{
    private readonly SchoolContext _context = context;
    private readonly DbSet<PaymentMethod> _dbSet = context.Set<PaymentMethod>();

    public async Task<PaymentMethod> AddAsync(PaymentMethod entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<PaymentMethod> UpdateAsync(PaymentMethod entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<PaymentMethod> DeleteAsync(PaymentMethod entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<PaymentMethod?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<List<PaymentMethod>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<(List<PaymentMethod> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null)
    {
        var query = _dbSet.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(pm => 
                pm.ProviderMethod.Contains(search) || 
                pm.MethodName.Contains(search));
        }

        var totalCount = await query.CountAsync();
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (data, totalCount);
    }

    public async Task<PaymentMethod> ActivateAsync(int id)
    {
        var method = await _dbSet.FindAsync(id);
        if (method != null)
        {
            method.IsActive = !method.IsActive;
            await _context.SaveChangesAsync();
        }
        return method!;
    }
}
