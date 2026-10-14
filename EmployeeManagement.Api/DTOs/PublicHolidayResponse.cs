namespace EmployeeManagement.Api.DTOs;

public class PublicHolidayResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public string? Description { get; set; }
}