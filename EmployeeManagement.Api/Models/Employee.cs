namespace EmployeeManagement.Api.Models
{
    public class Employee
    {
        public int Id { get; set; }

        public string EmployeeNo { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public int DepartmentId { get; set; }

        public Department Department { get; set; } = null!;

        public string Position { get; set; } = string.Empty;

        public DateTime JoinDate { get; set; }

        public bool IsActive { get; set; }

        public User? User { get; set; }

        public ICollection<LeaveRequest> LeaveRequests { get; set; }
    = new List<LeaveRequest>();
    }
}
