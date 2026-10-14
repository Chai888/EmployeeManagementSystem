
using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs;
using Microsoft.EntityFrameworkCore;
using System;

namespace EmployeeManagement.Api.Services;

public class LeaveBalanceService
{
    private readonly ApplicationDbContext _context;
    private readonly LeaveCalculationService _leaveCalculationService;

    public LeaveBalanceService(
        ApplicationDbContext context,
        LeaveCalculationService leaveCalculationService)
    {
        _context = context;
        _leaveCalculationService = leaveCalculationService;
    }

    public async Task<LeaveBalanceResponse?> GetByEmployeeAsync(
        int employeeId,
        int year)
    {
        var balance = await _context.LeaveBalances
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(x =>
                x.EmployeeId == employeeId &&
                x.Year == year &&
                x.LeaveType.Trim().ToLower() == "annual leave");

        if (balance == null)
        {
            return null;
        }

        var pendingRequests = await _context.LeaveRequests
            .Where(x =>
                x.EmployeeId == employeeId &&
                x.Status == "Pending" &&
                x.StartDate.Year == year &&
                x.EndDate.Year == year &&
                x.LeaveType.Trim().ToLower() == "annual leave")
            .ToListAsync();

        decimal pendingDays = 0;

        foreach (var pendingRequest in pendingRequests)
        {
            pendingDays += await _leaveCalculationService
                .CalculateWorkingDaysAsync(
                    pendingRequest.StartDate,
                    pendingRequest.EndDate);
        }

        var usedDays = balance.UsedDays + pendingDays;

        return new LeaveBalanceResponse
        {
            EmployeeId = balance.EmployeeId,
            EmployeeName = balance.Employee.Name,
            Year = balance.Year,
            LeaveType = balance.LeaveType,
            EntitledDays = balance.EntitledDays,
            UsedDays = Math.Min(balance.EntitledDays, usedDays),
            RemainingDays = Math.Max(
                0,
                balance.EntitledDays - usedDays)
        };
    }

    public async Task<List<LeaveBalanceResponse>> GetAllByEmployeeAsync(
        int employeeId,
        int year)
    {
        var balances = await _context.LeaveBalances
            .Include(x => x.Employee)
            .Where(x =>
                x.EmployeeId == employeeId &&
                x.Year == year)
            .OrderBy(x => x.LeaveType)
            .ToListAsync();

        var pendingRequests = await _context.LeaveRequests
            .Where(x =>
                x.EmployeeId == employeeId &&
                x.Status == "Pending" &&
                x.StartDate.Year == year &&
                x.EndDate.Year == year)
            .ToListAsync();

        var result = new List<LeaveBalanceResponse>();

        foreach (var balance in balances)
        {
            decimal pendingDays = 0;

            var sameTypePendingRequests = pendingRequests
                .Where(x => string.Equals(
                    x.LeaveType?.Trim(),
                    balance.LeaveType?.Trim(),
                    StringComparison.OrdinalIgnoreCase));

            foreach (var pendingRequest in sameTypePendingRequests)
            {
                pendingDays += await _leaveCalculationService
                    .CalculateWorkingDaysAsync(
                        pendingRequest.StartDate,
                        pendingRequest.EndDate);
            }

            var usedDays = balance.UsedDays + pendingDays;

            result.Add(new LeaveBalanceResponse
            {
                EmployeeId = balance.EmployeeId,
                EmployeeName = balance.Employee.Name,
                Year = balance.Year,
                LeaveType = balance.LeaveType,
                EntitledDays = balance.EntitledDays,
                UsedDays = Math.Min(balance.EntitledDays, usedDays),
                RemainingDays = Math.Max(
                    0,
                    balance.EntitledDays - usedDays)
            });
        }

        return result;
    }

}
