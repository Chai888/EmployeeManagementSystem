
namespace EmployeeManagement.Api.Models;

public class AuditLog
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public string? EntityId { get; set; }

    public string? Details { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}