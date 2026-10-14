
using System.ComponentModel.DataAnnotations;

namespace EmployeeManagement.Api.DTOs;

public class CreateDepartmentRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
}