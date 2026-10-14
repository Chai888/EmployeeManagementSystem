namespace EmployeeManagement.Api.DTOs
{
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public int? EmployeeId { get; set; }

    }
}
