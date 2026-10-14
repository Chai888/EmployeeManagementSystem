using EmployeeManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Department> Departments { get; set; }

        public DbSet<Employee> Employees { get; set; }

        public DbSet<LeaveRequest> LeaveRequests { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<LeaveBalance> LeaveBalances { get; set; }

        public DbSet<PublicHoliday> PublicHolidays { get; set; }

        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        protected override void OnModelCreating(
        ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Department>()
                .HasIndex(d => d.Name)
                .IsUnique();

            modelBuilder.Entity<PublicHoliday>()
                .Property(x => x.Name)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<PublicHoliday>()
                .Property(x => x.Description)
                .HasMaxLength(250);

            modelBuilder.Entity<PublicHoliday>()
                .HasIndex(x => x.Date)
                .IsUnique();

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Department)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.EmployeeNo)
                .IsUnique();

            modelBuilder.Entity<LeaveRequest>()
                .HasOne(l => l.Employee)
                .WithMany(e => e.LeaveRequests)
                .HasForeignKey(l => l.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LeaveRequest>()
                .HasIndex(x => new
                {
                    x.EmployeeId,
                    x.Status,
                    x.StartDate
                });

            modelBuilder.Entity<User>()
                .HasOne(u => u.Employee)
                .WithOne(e => e.User)
                .HasForeignKey<User>(u => u.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LeaveBalance>()
                .HasOne(x => x.Employee)
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LeaveBalance>()
                .HasIndex(x => new
                {
                    x.EmployeeId,
                    x.Year,
                    x.LeaveType
                })
                .IsUnique();

            modelBuilder.Entity<LeaveBalance>()
                .Property(x => x.LeaveType)
                .HasMaxLength(50)
                .HasDefaultValue("Annual Leave")
                .IsRequired();

            modelBuilder.Entity<LeaveBalance>()
                .Property(x => x.EntitledDays)
                .HasPrecision(5, 2);

            modelBuilder.Entity<LeaveBalance>()
                .Property(x => x.UsedDays)
                .HasPrecision(5, 2);

            modelBuilder.Entity<LeaveRequest>()
                .Property(x => x.RowVersion)
                .IsRowVersion();

            modelBuilder.Entity<LeaveBalance>()
                .Property(x => x.RowVersion)
                .IsRowVersion();
        }
    }
}
