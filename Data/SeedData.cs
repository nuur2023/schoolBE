using Microsoft.EntityFrameworkCore;
using schoolBE.Models;
using schoolBE.Utils;

namespace schoolBE.Data;

public static class SeedData
{
    public static void Seed(SchoolContext context)
    {
        // Ensure database is created and migrations are applied
        context.Database.EnsureCreated();
        // If using migrations, uncomment the line below instead:
        // context.Database.Migrate();

        // Seed Roles
        if (!context.Roles.Any())
        {
            var roles = new List<Role>
            {
                new Role { Name = "SuperAdmin", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Role { Name = "Admin", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Role { Name = "Teacher", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Role { Name = "Parent", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Role { Name = "Accountant", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            };
            context.Roles.AddRange(roles);
            context.SaveChanges();
        }

        // Seed SuperAdmin User
        if (!context.Users.Any(u => u.Email == "abaal@abaal.com"))
        {
            var superAdminUser = new User
            {
                Name = "Nuur Hassan",
                Email = "abaal@abaal.com",
                HashedPassword = Helper.HashPassword("abaal"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Users.Add(superAdminUser);
            context.SaveChanges();
        }
    }
}

