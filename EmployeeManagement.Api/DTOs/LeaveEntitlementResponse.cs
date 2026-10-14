
namespace EmployeeManagement.Api.DTOs;

public class LeaveEntitlementResponse
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public string EmployeeName { get; set; } = string.Empty;

    public int Year { get; set; }

    public string LeaveType { get; set; } = string.Empty;

    public decimal EntitledDays { get; set; }

    public decimal UsedDays { get; set; }

    public decimal RemainingDays { get; set; }
}
