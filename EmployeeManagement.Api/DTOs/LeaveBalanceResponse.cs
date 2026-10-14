namespace EmployeeManagement.Api.DTOs
{
    public class LeaveBalanceResponse
    {
        public int EmployeeId { get; set; }

        public string EmployeeName { get; set; } = string.Empty;

        public int Year { get; set; }

        public decimal EntitledDays { get; set; }

        public decimal UsedDays { get; set; }

        public decimal RemainingDays { get; set; }

        public string LeaveType { get; set; } = string.Empty;
    }
}
