
using System.ComponentModel.DataAnnotations;

namespace EmployeeManagement.Api.Models
{
    public class LeaveBalance
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        public Employee Employee { get; set; } = null!;

        public int Year { get; set; }

        public string LeaveType { get; set; } = "Annual Leave";

        public decimal EntitledDays { get; set; }

        public decimal UsedDays { get; set; }

        public decimal RemainingDays => EntitledDays - UsedDays;

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
