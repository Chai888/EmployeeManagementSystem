
using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.Models;

namespace EmployeeManagement.Api.Services;

public class AuditLogService
{
    private readonly ApplicationDbContext _context;

    public AuditLogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(
        int? userId,
        string username,
        string action,
        string entityType,
        string? entityId = null,
        string? details = null)
    {
        var log = new AuditLog
        {
            UserId = userId,
            Username = username,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);

        await _context.SaveChangesAsync();
    }
}