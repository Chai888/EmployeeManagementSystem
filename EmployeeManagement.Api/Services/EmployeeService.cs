using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs;
using EmployeeManagement.Api.Models;
using Microsoft.EntityFrameworkCore;
using EmployeeManagement.Api.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;

namespace EmployeeManagement.Api.Services;

public class EmployeeService
{
    private readonly ApplicationDbContext _context;
    private readonly AuditLogService _auditLogService;
    private readonly IPasswordHasher<User> _passwordHasher;

    public EmployeeService(
        ApplicationDbContext context,
        AuditLogService auditLogService,
        IPasswordHasher<User> passwordHasher)
    {
        _context = context;
        _auditLogService = auditLogService;
        _passwordHasher = passwordHasher;
    }

    public async Task<List<EmployeeResponse>> GetAllAsync()
    {
        return await _context.Employees
            .Include(e => e.Department)
            .Select(e => new EmployeeResponse
            {
                Id = e.Id,
                EmployeeNo = e.EmployeeNo,
                Name = e.Name,
                Email = e.Email,
                Department = e.Department.Name,
                Position = e.Position,
                JoinDate = e.JoinDate,
                IsActive = e.IsActive
            })
            .ToListAsync();
    }

    public async Task<EmployeeResponse?> GetByIdAsync(int id)
    {
        return await _context.Employees
            .Include(e => e.Department)
            .Where(e => e.Id == id)
            .Select(e => new EmployeeResponse
            {
                Id = e.Id,
                EmployeeNo = e.EmployeeNo,
                Name = e.Name,
                Email = e.Email,
                Department = e.Department.Name,
                Position = e.Position,
                JoinDate = e.JoinDate,
                IsActive = e.IsActive
            })
            .FirstOrDefaultAsync();
    }

    public async Task<PagedResponse<EmployeeResponse>> SearchAsync(
        string? search,
        int? departmentId,
        bool? isActive,
        int page,
        int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();

            query = query.Where(e =>
                e.Name.Contains(keyword) ||
                e.EmployeeNo.Contains(keyword));
        }

        if (departmentId.HasValue)
        {
            query = query.Where(e =>
                e.DepartmentId == departmentId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(e =>
                e.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync();

        var employees = await query
            .OrderBy(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EmployeeResponse
            {
                Id = e.Id,
                EmployeeNo = e.EmployeeNo,
                Name = e.Name,
                Email = e.Email,
                Department = e.Department.Name,
                Position = e.Position,
                JoinDate = e.JoinDate,
                IsActive = e.IsActive
            })
            .ToListAsync();

        return new PagedResponse<EmployeeResponse>
        {
            Items = employees,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<CreateEmployeeWithAccountResponse> CreateAsync(
        CreateEmployeeRequest request,
        int? actorUserId,
        string actorUsername)
    {
        var exists = await _context.Employees
            .AnyAsync(e => e.EmployeeNo == request.EmployeeNo);

        if (exists)
        {
            throw new InvalidOperationException(
                $"Employee number '{request.EmployeeNo}' already exists.");
        }

        var username = request.EmployeeNo.Trim();

        var usernameExists = await _context.Users
            .AnyAsync(u => u.Username == username);

        if (usernameExists)
        {
            throw new InvalidOperationException(
                $"Username '{username}' already exists.");
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        var employee = new Employee
        {
            EmployeeNo = request.EmployeeNo,
            Name = request.Name,
            Email = request.Email,
            DepartmentId = request.DepartmentId,
            Position = request.Position,
            JoinDate = request.JoinDate,
            IsActive = request.IsActive
        };


        _context.Employees.Add(employee);

        // Save first so the employee receives a database ID.
        await _context.SaveChangesAsync();

        var temporaryPassword =
            Convert.ToHexString(RandomNumberGenerator.GetBytes(16));

        var user = new User
        {
            Username = employee.EmployeeNo.Trim(),
            Email = employee.Email.Trim(),
            EmployeeId = employee.Id,
            Role = "Employee",
            IsActive = employee.IsActive
        };

        user.PasswordHash = _passwordHasher.HashPassword(
            user,
            temporaryPassword);

        _context.Users.Add(user);

        await _context.SaveChangesAsync();

        var createdEmployee = await _context.Employees
            .Include(e => e.Department)
            .FirstAsync(e => e.Id == employee.Id);

        await _auditLogService.LogAsync(
            actorUserId,
            actorUsername,
            "EmployeeCreated",
            "Employee",
            createdEmployee.Id.ToString(),
            $"Created employee. " +
            $"Employee No: {createdEmployee.EmployeeNo}; " +
            $"Name: {createdEmployee.Name}; " +
            $"Email: {createdEmployee.Email}; " +
            $"Department: {createdEmployee.Department.Name}; " +
            $"Position: {createdEmployee.Position}");

        await transaction.CommitAsync();

        return new CreateEmployeeWithAccountResponse
        {
            Id = createdEmployee.Id,
            EmployeeNo = createdEmployee.EmployeeNo,
            Name = createdEmployee.Name,
            Email = createdEmployee.Email,
            Department = createdEmployee.Department.Name,
            Position = createdEmployee.Position,
            JoinDate = createdEmployee.JoinDate,
            IsActive = createdEmployee.IsActive,
            Username = user.Username,
            TemporaryPassword = temporaryPassword
        };
    }

    public async Task<EmployeeResponse?> UpdateAsync(
        int id,
        UpdateEmployeeRequest request,
        int? actorUserId,
        string actorUsername)
    {
        var employee = await _context.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
        {
            return null;
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        var oldEmployeeNo = employee.EmployeeNo;
        var oldName = employee.Name;
        var oldEmail = employee.Email;
        var oldDepartment = employee.Department.Name;
        var oldPosition = employee.Position;
        var oldJoinDate = employee.JoinDate;
        var oldIsActive = employee.IsActive;

        employee.EmployeeNo = request.EmployeeNo;
        employee.Name = request.Name;
        employee.Email = request.Email;
        employee.DepartmentId = request.DepartmentId;
        employee.Position = request.Position;
        employee.JoinDate = request.JoinDate;
        employee.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        await _context.Entry(employee)
            .Reference(e => e.Department)
            .LoadAsync();

        var details =
            $"Updated employee. " +
            $"Employee No: {oldEmployeeNo} -> {employee.EmployeeNo}; " +
            $"Name: {oldName} -> {employee.Name}; " +
            $"Email: {oldEmail} -> {employee.Email}; " +
            $"Department: {oldDepartment} -> {employee.Department?.Name}; " +
            $"Position: {oldPosition} -> {employee.Position}; " +
            $"Join Date: {oldJoinDate:yyyy-MM-dd} -> {employee.JoinDate:yyyy-MM-dd}; " +
            $"Active: {oldIsActive} -> {employee.IsActive}";

        await _auditLogService.LogAsync(
            actorUserId,
            actorUsername,
            "EmployeeUpdated",
            "Employee",
            employee.Id.ToString(),
            details);

        await transaction.CommitAsync();

        return new EmployeeResponse
        {
            Id = employee.Id,
            EmployeeNo = employee.EmployeeNo,
            Name = employee.Name,
            Email = employee.Email,
            Department = employee.Department?.Name ?? "",
            Position = employee.Position,
            JoinDate = employee.JoinDate,
            IsActive = employee.IsActive
        };
    }

    public async Task<bool> DeleteAsync(
        int id,
        int? actorUserId,
        string actorUsername)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
        {
            return false;
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        var employeeNo = employee.EmployeeNo;
        var employeeName = employee.Name;
        var employeeEmail = employee.Email;

        // Find the login account linked to this employee.
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.EmployeeId == employee.Id);

        // Deactivate the account instead of permanently deleting it.
        if (user != null)
        {
            user.IsActive = false;
        }

        // Delete the employee.
        _context.Employees.Remove(employee);

        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            actorUserId,
            actorUsername,
            "EmployeeDeleted",
            "Employee",
            id.ToString(),
            $"Deleted employee. " +
            $"Employee No: {employeeNo}; " +
            $"Name: {employeeName}; " +
            $"Email: {employeeEmail}; " +
            $"Login Account Deactivated: {user != null}");

        await transaction.CommitAsync();

        return true;
    }
}
