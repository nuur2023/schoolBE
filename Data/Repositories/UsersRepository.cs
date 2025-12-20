using Microsoft.EntityFrameworkCore;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Data.Repositories;

public class UsersRepository(SchoolContext context) : IUsers
{
    private readonly SchoolContext _context = context;
    private readonly DbSet<User> _dbSet = context.Set<User>();

    public async Task<User> AddAsync(User entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<User> UpdateAsync(User entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<User> DeleteAsync(User entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<List<User>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<(List<User> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null)
    {
        var query = _dbSet.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(u => 
                u.Name.Contains(search) || 
                u.Email.Contains(search));
        }

        var totalCount = await query.CountAsync();
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (data, totalCount);
    }

    public async Task<User> ActivateAsync(int id)
    {
        var user = await _dbSet.FindAsync(id);
        if (user != null)
        {
            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();
        }
        return user!;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
    }
}
