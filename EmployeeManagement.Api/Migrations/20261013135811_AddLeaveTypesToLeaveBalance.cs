using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmployeeManagement.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveTypesToLeaveBalance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LeaveBalances_EmployeeId_Year",
                table: "LeaveBalances");

            migrationBuilder.AddColumn<string>(
                name: "LeaveType",
                table: "LeaveBalances",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Annual Leave");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalances_EmployeeId_Year_LeaveType",
                table: "LeaveBalances",
                columns: new[] { "EmployeeId", "Year", "LeaveType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LeaveBalances_EmployeeId_Year_LeaveType",
                table: "LeaveBalances");

            migrationBuilder.DropColumn(
                name: "LeaveType",
                table: "LeaveBalances");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalances_EmployeeId_Year",
                table: "LeaveBalances",
                columns: new[] { "EmployeeId", "Year" },
                unique: true);
        }
    }
}
