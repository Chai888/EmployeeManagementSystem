using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs;
using EmployeeManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Api.Services;

public class LeaveRequestService
{
    private readonly ApplicationDbContext _context;
    private readonly LeaveCalculationService _leaveCalculationService;
    private readonly AuditLogService _auditLogService;

    public LeaveRequestService(
        ApplicationDbContext context,
        LeaveCalculationService leaveCalculationService,
        AuditLogService auditLogService)
    {
        _context = context;
        _leaveCalculationService = leaveCalculationService;
        _auditLogService = auditLogService;
    }
    public async Task<List<LeaveRequestResponse>> GetAllAsync(
        string? status)
    {
        var query = _context.LeaveRequests
            .Include(x => x.Employee)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        return await query
            .OrderByDescending(x => x.Id)
            .Select(x => new LeaveRequestResponse
            {
                Id = x.Id,
                EmployeeId = x.EmployeeId,
                EmployeeName = x.Employee.Name,
                LeaveType = x.LeaveType,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                Reason = x.Reason,
                Status = x.Status,
                ApprovedBy = x.ApprovedBy,
                ApprovedDate = x.ApprovedDate
            })
            .ToListAsync();
    }

    public async Task<LeaveRequestResponse?> GetByIdAsync(int id)
    {
        return await _context.LeaveRequests
            .Include(x => x.Employee)
            .Where(x => x.Id == id)
            .Select(x => new LeaveRequestResponse
            {
                Id = x.Id,
                EmployeeId = x.EmployeeId,
                EmployeeName = x.Employee.Name,
                LeaveType = x.LeaveType,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                Reason = x.Reason,
                Status = x.Status,
                ApprovedBy = x.ApprovedBy,
                ApprovedDate = x.ApprovedDate
            })
            .FirstOrDefaultAsync();
    }

    public async Task<List<LeaveRequestResponse>> GetMyLeaveRequestsAsync(int userId)
    {
        return await _context.LeaveRequests
            .Include(x => x.Employee)
            .Where(x => x.Employee.User != null &&
                        x.Employee.User.Id == userId)
            .OrderByDescending(x => x.Id)
            .Select(x => new LeaveRequestResponse
            {
                Id = x.Id,
                EmployeeId = x.EmployeeId,
                EmployeeName = x.Employee.Name,
                LeaveType = x.LeaveType,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                Reason = x.Reason,
                Status = x.Status,
                ApprovedBy = x.ApprovedBy,
                ApprovedDate = x.ApprovedDate
            })
            .ToListAsync();
    }




    public async Task<LeaveRequestResponse> CreateMyAsync(
        int userId,
        CreateLeaveRequest request)
    {
        if (request.EndDate.Date < request.StartDate.Date)
        {
            throw new InvalidOperationException(
                "End date cannot be earlier than start date.");
        }

        if (request.StartDate.Year != request.EndDate.Year)
        {
            throw new InvalidOperationException(
                "Leave requests cannot cross calendar years.");
        }

        if (string.IsNullOrWhiteSpace(request.LeaveType))
        {
            throw new InvalidOperationException("Leave type is required.");
        }

        await using var transaction =
        await _context.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable);

        var leaveType = System.Globalization.CultureInfo
            .InvariantCulture.TextInfo
            .ToTitleCase(
                request.LeaveType.Trim().ToLowerInvariant());

        var user = await _context.Users
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user?.Employee == null)
        {
            throw new KeyNotFoundException(
                "Employee profile was not found.");
        }

        var employee = user.Employee;
        var startDate = request.StartDate.Date;
        var endDate = request.EndDate.Date;
        var year = startDate.Year;

        var hasOverlappingLeave = await _context.LeaveRequests
            .AnyAsync(x =>
                x.EmployeeId == employee.Id &&
                (x.Status == "Pending" || x.Status == "Approved") &&
                startDate <= x.EndDate &&
                endDate >= x.StartDate);

        if (hasOverlappingLeave)
        {
            throw new InvalidOperationException(
                "You already have a pending or approved leave request " +
                "that overlaps with the selected dates.");
        }

        var matchingBalances = await _context.LeaveBalances
            .Where(x =>
                x.EmployeeId == employee.Id &&
                x.Year == year &&
                x.LeaveType.Trim().ToLower() ==
                    leaveType.Trim().ToLower())
            .ToListAsync();

        if (matchingBalances.Count > 1)
        {
            throw new InvalidOperationException(
                "Duplicate leave balance records exist for this employee, year and leave type. Please contact the administrator.");
        }

        var balance = matchingBalances.SingleOrDefault();

        if (balance == null)
        {
            throw new InvalidOperationException(
                $"No {leaveType} entitlement has been configured for {year}.");
        }

        var requestedDays = await _leaveCalculationService
            .CalculateWorkingDaysAsync(startDate, endDate);

        if (requestedDays <= 0)
        {
            throw new InvalidOperationException(
                "Leave request must include at least one working day.");
        }

        var pendingRequests = await _context.LeaveRequests
            .Where(x =>
                x.EmployeeId == employee.Id &&
                x.LeaveType.Trim().ToLower() ==
                    leaveType.Trim().ToLower() &&
                x.Status == "Pending" &&
                x.StartDate.Year == year)
            .ToListAsync();

        decimal pendingDays = 0;

        foreach (var pendingRequest in pendingRequests)
        {
            pendingDays += await _leaveCalculationService.CalculateWorkingDaysAsync(
                pendingRequest.StartDate,
                pendingRequest.EndDate);
        }

        var availableDays = balance.RemainingDays - pendingDays;

        if (requestedDays > availableDays)
        {
            throw new InvalidOperationException(
                $"Insufficient {leaveType} balance. " +
                $"Available for new requests: " +
                $"{Math.Max(0, availableDays)} day(s).");
        }

        var leaveRequest = new LeaveRequest
        {
            EmployeeId = employee.Id,
            LeaveType = leaveType,
            StartDate = startDate,
            EndDate = endDate,
            Reason = request.Reason.Trim(),
            Status = "Pending"
        };

        _context.LeaveRequests.Add(leaveRequest);

        // Save the request first to generate its ID.
        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            user.Id,
            user.Username,
            "Applied",
            "Leave",
            leaveRequest.Id.ToString(),
            $"Employee: {employee.Name}; " +
            $"Leave type: {leaveRequest.LeaveType}; " +
            $"Start date: {leaveRequest.StartDate:yyyy-MM-dd}; " +
            $"End date: {leaveRequest.EndDate:yyyy-MM-dd}; " +
            $"Requested days: {requestedDays}; " +
            $"Status: Pending; " +
            $"Reason: {leaveRequest.Reason}");

        await transaction.CommitAsync();

        return new LeaveRequestResponse
        {
            Id = leaveRequest.Id,
            EmployeeId = employee.Id,
            EmployeeName = employee.Name,
            LeaveType = leaveRequest.LeaveType,
            StartDate = leaveRequest.StartDate,
            EndDate = leaveRequest.EndDate,
            Reason = leaveRequest.Reason,
            Status = leaveRequest.Status
        };
    }



    public async Task<LeaveRequestResponse?> UpdateStatusAsync(
        int id,
        string status,
        int approvedBy,
        string approvedByUsername)
    {
        if (status != "Approved" && status != "Rejected")
        {
            throw new InvalidOperationException(
                "Status must be Approved or Rejected.");
        }

        await using var transaction =
    await _context.Database.BeginTransactionAsync(
        System.Data.IsolationLevel.Serializable);

        var leaveRequest = await _context.LeaveRequests
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (leaveRequest == null)
        {
            await transaction.RollbackAsync();
            return null;
        }

        if (leaveRequest.Status != "Pending")
        {
            throw new InvalidOperationException(
                "Only pending leave requests can be approved or rejected.");
        }

        if (status == "Approved")
        {
            var year = leaveRequest.StartDate.Year;

            // Load this employee's leave balances for the year.
            var employeeBalances = await _context.LeaveBalances
                .Where(x =>
                    x.EmployeeId == leaveRequest.EmployeeId &&
                    x.Year == year)
                .ToListAsync();

            // Match leave type regardless of casing or surrounding spaces.
            var matchingBalances = employeeBalances
                .Where(x => string.Equals(
                    x.LeaveType?.Trim(),
                    leaveRequest.LeaveType?.Trim(),
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matchingBalances.Count > 1)
            {
                throw new InvalidOperationException(
                    "Duplicate leave balance records exist for this employee, year and leave type. Please resolve the duplicates before approving this request.");
            }

            var balance = matchingBalances.SingleOrDefault();

            if (balance == null)
            {
                throw new InvalidOperationException(
                    $"No {leaveRequest.LeaveType} entitlement has been configured for {year}.");
            }

            // Use the canonical leave type stored in the balance.
            leaveRequest.LeaveType = balance.LeaveType;

            var requestedDays = await _leaveCalculationService
                .CalculateWorkingDaysAsync(
                    leaveRequest.StartDate,
                    leaveRequest.EndDate);

            // Load other pending requests and compare leave types in memory.
            var otherPendingRequests = await _context.LeaveRequests
                .Where(x =>
                    x.EmployeeId == leaveRequest.EmployeeId &&
                    x.Id != leaveRequest.Id &&
                    x.Status == "Pending" &&
                    x.StartDate.Year == year)
                .ToListAsync();

            var sameTypePendingRequests = otherPendingRequests
                .Where(x => string.Equals(
                    x.LeaveType?.Trim(),
                    balance.LeaveType?.Trim(),
                    StringComparison.OrdinalIgnoreCase));

            decimal otherPendingDays = 0;

            foreach (var pendingRequest in sameTypePendingRequests)
            {
                otherPendingDays += await _leaveCalculationService
                    .CalculateWorkingDaysAsync(
                        pendingRequest.StartDate,
                        pendingRequest.EndDate);
            }

            var availableDays =
                balance.RemainingDays - otherPendingDays;

            if (requestedDays > availableDays)
            {
                throw new InvalidOperationException(
                    $"Insufficient {leaveRequest.LeaveType} balance " +
                    "to approve this request.");
            }

            balance.UsedDays += requestedDays;
        }

        leaveRequest.Status = status;
        leaveRequest.ApprovedBy = approvedBy;
        leaveRequest.ApprovedDate = DateTime.UtcNow;

        // Save the leave request and leave balance changes.
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync();

            throw new InvalidOperationException(
                "This leave request or leave balance was modified by " +
                "another operation. Please refresh the data and try again.",
                ex);
        }

        // Save the audit log.
        await _auditLogService.LogAsync(
            approvedBy,
            approvedByUsername,
            status,
            "Leave",
            leaveRequest.Id.ToString(),
            $"Employee: {leaveRequest.Employee.Name}; " +
            $"Leave type: {leaveRequest.LeaveType}; " +
            $"Start date: {leaveRequest.StartDate:yyyy-MM-dd}; " +
            $"End date: {leaveRequest.EndDate:yyyy-MM-dd}; " +
            $"Decision: {status}; " +
            $"Reason: {leaveRequest.Reason}");

        await transaction.CommitAsync();

        return new LeaveRequestResponse
        {
            Id = leaveRequest.Id,
            EmployeeId = leaveRequest.EmployeeId,
            EmployeeName = leaveRequest.Employee.Name,
            LeaveType = leaveRequest.LeaveType,
            StartDate = leaveRequest.StartDate,
            EndDate = leaveRequest.EndDate,
            Reason = leaveRequest.Reason,
            Status = leaveRequest.Status,
            ApprovedBy = leaveRequest.ApprovedBy,
            ApprovedDate = leaveRequest.ApprovedDate
        };
    }

    public async Task<bool> IsOwnerAsync(
    int leaveRequestId,
    int userId)
    {
        return await _context.LeaveRequests
            .AnyAsync(x =>
                x.Id == leaveRequestId &&
                x.Employee.User != null &&
                x.Employee.User.Id == userId);
    }
}