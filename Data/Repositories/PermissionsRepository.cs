using Microsoft.EntityFrameworkCore;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using schoolBE.Models;
using schoolBE.ViewModels;

namespace schoolBE.Data.Repositories;

public class PermissionsRepository(SchoolContext context) : IPermissions
{
    private readonly SchoolContext _context = context;
    private readonly DbSet<Permission> _dbSet = context.Set<Permission>();

    public async Task<Permission> AddAsync(Permission entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Permission> UpdateAsync(Permission entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Permission> DeleteAsync(Permission entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Permission?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<List<Permission>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<(List<Permission> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null)
    {
        var query = _dbSet.AsQueryable();

        var totalCount = await query.CountAsync();
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (data, totalCount);
    }

    public async Task<List<PermissionVM>> GetUserPermissionsAsync(int userId, int schoolId)
    {
        var permissions = await _dbSet
            .Where(p => p.UsersId == userId && p.SchoolId == schoolId)
            .ToListAsync();

        return permissions.Select(p => new PermissionVM
        {
            Id = p.Id,
            MenuId = p.MenuId,
            SubmenuId = p.SubmenuId,
            UsersId = p.UsersId,
            SchoolId = p.SchoolId,
            CanAccessMenu = p.CanAccessMenu,
            CanAccessSubmenu = p.CanAccessSubmenu,
            CanCreate = p.CanCreate,
            CanUpdate = p.CanUpdate,
            CanView = p.CanView,
            CanDelete = p.CanDelete
        }).ToList();
    }

    public async Task SaveUserPermissionsAsync(int userId, int schoolId, List<PermissionVM> permissions)
    {
        // Remove existing permissions for this user and school
        var existing = await _dbSet
            .Where(p => p.UsersId == userId && p.SchoolId == schoolId)
            .ToListAsync();
        
        _dbSet.RemoveRange(existing);

        // Add new permissions
        var newPermissions = permissions.Select(p => new Permission
        {
            MenuId = p.MenuId,
            SubmenuId = p.SubmenuId,
            UsersId = userId,
            SchoolId = schoolId,
            CanAccessMenu = p.CanAccessMenu,
            CanAccessSubmenu = p.CanAccessSubmenu,
            CanCreate = p.CanCreate,
            CanUpdate = p.CanUpdate,
            CanView = p.CanView,
            CanDelete = p.CanDelete
        }).ToList();

        await _dbSet.AddRangeAsync(newPermissions);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteForMenuAsync(int menuId)
    {
        var subIds = await _context.Set<Submenu>()
            .Where(s => s.MenuId == menuId)
            .Select(s => s.Id)
            .ToListAsync();
        var perms = await _dbSet
            .Where(p =>
                p.MenuId == menuId ||
                (p.SubmenuId.HasValue && subIds.Contains(p.SubmenuId.Value)))
            .ToListAsync();
        if (perms.Count == 0) return;
        _dbSet.RemoveRange(perms);
        await _context.SaveChangesAsync();
    }
}
