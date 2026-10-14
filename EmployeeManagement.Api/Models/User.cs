namespace EmployeeManagement.Api.Models
{
    public class User
    {
        public int Id { get; set; }

        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string Role { get; set; } = "Employee";

        public bool IsActive { get; set; } = true;

        public int? EmployeeId { get; set; }

        public Employee? Employee { get; set; }
    }
}
