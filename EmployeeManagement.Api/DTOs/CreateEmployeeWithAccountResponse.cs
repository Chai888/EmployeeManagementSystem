
using System;

namespace EmployeeManagement.Api.DTOs;

public class CreateEmployeeWithAccountResponse
{
    public int Id { get; set; }

    public string EmployeeNo { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public string Position { get; set; } = string.Empty;

    public DateTime JoinDate { get; set; }

    public bool IsActive { get; set; }

    public string Username { get; set; } = string.Empty;

    public string TemporaryPassword { get; set; } = string.Empty;
}
