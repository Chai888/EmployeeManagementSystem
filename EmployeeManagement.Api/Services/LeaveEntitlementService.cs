using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs;
using EmployeeManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Api.Services;

public class LeaveEntitlementService
{
    private readonly ApplicationDbContext _context;
    private readonly AuditLogService _auditLogService;

    public LeaveEntitlementService(
        ApplicationDbContext context,
        AuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
    }

    public async Task<List<LeaveEntitlementResponse>>
        GetForEmployeeAsync(int employeeId, int year)
    {
        return await _context.LeaveBalances
            .Where(x =>
                x.EmployeeId == employeeId &&
                x.Year == year)
            .OrderBy(x => x.LeaveType)
            .Select(x => new LeaveEntitlementResponse
            {
                Id = x.Id,
                EmployeeId = x.EmployeeId,
                EmployeeName = x.Employee.Name,
                Year = x.Year,
                LeaveType = x.LeaveType,
                EntitledDays = x.EntitledDays,
                UsedDays = x.UsedDays,
                RemainingDays = x.RemainingDays
            })
            .ToListAsync();
    }

    public async Task<LeaveEntitlementResponse> SaveAsync(
        int employeeId,
        LeaveEntitlementRequest request,
        int? actorUserId,
        string actorUsername)
    {
        if (string.IsNullOrWhiteSpace(request.LeaveType))
        {
            throw new InvalidOperationException(
                "Leave type is required.");
        }

        // Normalize spaces and casing.
        var leaveType = System.Globalization.CultureInfo
            .InvariantCulture.TextInfo
            .ToTitleCase(request.LeaveType.Trim().ToLowerInvariant());

        var employee = await _context.Employees
            .FirstOrDefaultAsync(x => x.Id == employeeId);

        if (employee == null)
        {
            throw new KeyNotFoundException(
                "Employee was not found.");
        }

        // Match regardless of casing or leading/trailing spaces.
        var matchingBalances = await _context.LeaveBalances
            .Where(x =>
                x.EmployeeId == employeeId &&
                x.Year == request.Year &&
                x.LeaveType.Trim().ToLower() ==
                    leaveType.ToLower())
            .ToListAsync();

        // Detect duplicate records that may already exist in the database.
        if (matchingBalances.Count > 1)
        {
            throw new InvalidOperationException(
                "Duplicate leave balance records already exist for this employee, year and leave type. Please resolve the duplicates before updating the entitlement.");
        }

        var balance = matchingBalances.SingleOrDefault();
        var isNew = balance == null;

        decimal? previousEntitledDays = balance?.EntitledDays;
        decimal? previousUsedDays = balance?.UsedDays;

        if (balance == null)
        {
            balance = new LeaveBalance
            {
                EmployeeId = employeeId,
                Year = request.Year,
                LeaveType = leaveType,
                EntitledDays = request.EntitledDays,
                UsedDays = 0
            };

            _context.LeaveBalances.Add(balance);
        }
        else
        {
            if (request.EntitledDays < balance.UsedDays)
            {
                throw new InvalidOperationException(
                    "Entitlement cannot be lower than leave days already used.");
            }

            // Standardize the casing of an existing record as well.
            balance.LeaveType = leaveType;
            balance.EntitledDays = request.EntitledDays;
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        await _context.SaveChangesAsync();

        var action = isNew
            ? "EntitlementCreated"
            : "EntitlementUpdated";

        var details = isNew
            ? $"Assigned leave entitlement. " +
              $"Employee: {employee.Name}; " +
              $"Year: {balance.Year}; " +
              $"Leave type: {balance.LeaveType}; " +
              $"Entitled days: {balance.EntitledDays}; " +
              $"Used days: {balance.UsedDays}; " +
              $"Remaining days: {balance.RemainingDays}"
            : $"Updated leave entitlement. " +
              $"Employee: {employee.Name}; " +
              $"Year: {balance.Year}; " +
              $"Leave type: {balance.LeaveType}; " +
              $"Entitled days: {previousEntitledDays} -> {balance.EntitledDays}; " +
              $"Used days: {previousUsedDays} -> {balance.UsedDays}; " +
              $"Remaining days: {balance.RemainingDays}";

        await _auditLogService.LogAsync(
            actorUserId,
            actorUsername,
            action,
            "LeaveEntitlement",
            balance.Id.ToString(),
            details);

        await transaction.CommitAsync();

        return new LeaveEntitlementResponse
        {
            Id = balance.Id,
            EmployeeId = employee.Id,
            EmployeeName = employee.Name,
            Year = balance.Year,
            LeaveType = balance.LeaveType,
            EntitledDays = balance.EntitledDays,
            UsedDays = balance.UsedDays,
            RemainingDays = balance.RemainingDays
        };
    }
}
