using EmployeeManagement.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Api.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(
        ApplicationDbContext context,
        IConfiguration configuration)
    {
        await context.Database.MigrateAsync();

        var passwordHasher = new PasswordHasher<User>();

        // Create Admin
        if (!await context.Users.AnyAsync(x => x.Username == "admin"))
        {
            var admin = new User
            {
                Username = configuration["Admin:Username"] ?? "admin",
                Email = "admin@example.com",
                Role = "Admin",
                IsActive = true
            };

            var adminPassword =
                configuration["Admin:Password"]
                ?? throw new InvalidOperationException(
                    "Admin password is not configured.");

            admin.PasswordHash =
                passwordHasher.HashPassword(
                    admin,
                    adminPassword);

            context.Users.Add(admin);
        }

        // Create John employee login
        var employee = await context.Employees
            .FirstOrDefaultAsync(x => x.EmployeeNo == "EMP001");

        if (employee != null &&
            !await context.Users.AnyAsync(x => x.EmployeeId == employee.Id))
        {
            var john = new User
            {
                Username = "john",
                Email = employee.Email,
                Role = "Employee",
                IsActive = true,
                EmployeeId = employee.Id
            };

            john.PasswordHash =
                passwordHasher.HashPassword(
                    john,
                    "John123!");

            context.Users.Add(john);
        }

        await context.SaveChangesAsync();
    }
}