using System.ComponentModel.DataAnnotations;

namespace EmployeeManagement.Api.DTOs;

public class PublicHolidayRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    [MaxLength(250)]
    public string? Description { get; set; }
}