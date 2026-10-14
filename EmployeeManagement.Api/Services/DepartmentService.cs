using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs;
using EmployeeManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Api.Services;

public class DepartmentService
{
    private readonly ApplicationDbContext _context;
    private readonly AuditLogService _auditLogService;

    public DepartmentService(
        ApplicationDbContext context,
        AuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
    }

    public async Task<List<DepartmentResponse>> GetAllAsync()
    {
        return await _context.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentResponse
            {
                Id = d.Id,
                Name = d.Name,
                EmployeeCount = d.Employees.Count()
            })
            .ToListAsync();
    }

    public async Task<DepartmentResponse?> GetByIdAsync(int id)
    {
        return await _context.Departments
            .AsNoTracking()
            .Where(d => d.Id == id)
            .Select(d => new DepartmentResponse
            {
                Id = d.Id,
                Name = d.Name,
                EmployeeCount = d.Employees.Count()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<DepartmentResponse> CreateAsync(
        CreateDepartmentRequest request,
        int? actorUserId,
        string actorUsername)
    {
        var name = request.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Department name is required.");
        }

        var exists = await _context.Departments
            .AnyAsync(d => d.Name.ToLower() == name.ToLower());

        if (exists)
        {
            throw new InvalidOperationException(
                "Department name already exists.");
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        var department = new Department
        {
            Name = name
        };

        _context.Departments.Add(department);

        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            actorUserId,
            actorUsername,
            "DepartmentCreated",
            "Department",
            department.Id.ToString(),
            $"Created department. Name: {department.Name}");

        await transaction.CommitAsync();

        return new DepartmentResponse
        {
            Id = department.Id,
            Name = department.Name,
            EmployeeCount = 0
        };
    }

    public async Task<DepartmentResponse> UpdateAsync(
        int id,
        CreateDepartmentRequest request,
        int? actorUserId,
        string actorUsername)
    {
        var name = request.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Department name is required.");
        }

        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == id);

        if (department == null)
        {
            throw new KeyNotFoundException(
                "Department not found.");
        }

        var duplicateExists = await _context.Departments
            .AnyAsync(d =>
                d.Id != id &&
                d.Name.ToLower() == name.ToLower());

        if (duplicateExists)
        {
            throw new InvalidOperationException(
                "Department name already exists.");
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        var oldName = department.Name;

        department.Name = name;

        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            actorUserId,
            actorUsername,
            "DepartmentUpdated",
            "Department",
            department.Id.ToString(),
            $"Updated department name. " +
            $"Old name: {oldName}; New name: {department.Name}");

        await transaction.CommitAsync();

        return new DepartmentResponse
        {
            Id = department.Id,
            Name = department.Name,
            EmployeeCount = await _context.Employees
                .CountAsync(e => e.DepartmentId == id)
        };
    }

    public async Task DeleteAsync(
        int id,
        int? actorUserId,
        string actorUsername)
    {
        var department = await _context.Departments
            .Include(d => d.Employees)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (department == null)
        {
            throw new KeyNotFoundException(
                "Department not found.");
        }

        if (department.Employees.Any())
        {
            throw new InvalidOperationException(
                "Cannot delete a department that has employees.");
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        var departmentName = department.Name;

        _context.Departments.Remove(department);

        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            actorUserId,
            actorUsername,
            "DepartmentDeleted",
            "Department",
            id.ToString(),
            $"Deleted department. Name: {departmentName}");

        await transaction.CommitAsync();
    }
}
