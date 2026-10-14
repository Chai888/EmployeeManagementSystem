using System.ComponentModel.DataAnnotations;

namespace EmployeeManagement.Api.Models;

public class PublicHoliday
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    [MaxLength(250)]
    public string? Description { get; set; }
}