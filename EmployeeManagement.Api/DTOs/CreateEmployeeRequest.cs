using System.ComponentModel.DataAnnotations;

namespace EmployeeManagement.Api.DTOs;

public class CreateEmployeeRequest
{
    [Required]
    [MaxLength(20)]
    public string EmployeeNo { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public int DepartmentId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Position { get; set; } = string.Empty;

    [Required]
    public DateTime JoinDate { get; set; }

    public bool IsActive { get; set; } = true;
}