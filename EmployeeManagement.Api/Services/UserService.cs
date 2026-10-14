
using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Api.Services;

public class UserService
{
    private readonly ApplicationDbContext _context;

    public UserService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<EmployeeAccountResponse>>
        GetEmployeeAccountsAsync()
    {
        return await _context.Users
            .AsNoTracking()
            .Where(u => u.EmployeeId != null)
            .Select(u => new EmployeeAccountResponse
            {
                UserId = u.Id,
                EmployeeId = u.EmployeeId!.Value,
                EmployeeNo = u.Employee!.EmployeeNo,
                EmployeeName = u.Employee.Name,
                Username = u.Username,
                Email = u.Email,
                Role = u.Role,
                IsActive = u.IsActive
            })
            .OrderBy(u => u.EmployeeNo)
            .ToListAsync();
    }

    public async Task SetAccountStatusAsync(
        int userId,
        bool isActive)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            throw new KeyNotFoundException(
                "User account not found.");
        }

        if (user.Role == "Admin" && !isActive)
        {
            throw new InvalidOperationException(
                "Admin accounts cannot be deactivated through this endpoint.");
        }

        user.IsActive = isActive;

        await _context.SaveChangesAsync();
    }
}