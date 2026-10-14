
using System.ComponentModel.DataAnnotations;

namespace EmployeeManagement.Api.DTOs;

public class LeaveEntitlementRequest
{
    [Required]
    [Range(2000, 2100)]
    public int Year { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string LeaveType { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "365")]
    public decimal EntitledDays { get; set; }
}
