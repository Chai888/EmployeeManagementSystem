using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.Models;
using EmployeeManagement.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Tests;

public class LeaveCalculationServiceTests
{
    private static (ApplicationDbContext Context, LeaveCalculationService Service)
        CreateService()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"LeaveCalculationTests_{Guid.NewGuid():N}")
            .Options;

        var context = new ApplicationDbContext(options);
        var service = new LeaveCalculationService(context);

        return (context, service);
    }

    [Fact]
    public async Task CalculateWorkingDays_MondayToWednesday_ReturnsThree()
    {
        var (context, service) = CreateService();
        using (context)
        {
            var result = await service.CalculateWorkingDaysAsync(
                new DateTime(2026, 10, 12),
                new DateTime(2026, 10, 14));

            Assert.Equal(3m, result);
        }
    }

    [Fact]
    public async Task CalculateWorkingDays_SaturdayToMonday_ReturnsOne()
    {
        var (context, service) = CreateService();
        using (context)
        {
            var result = await service.CalculateWorkingDaysAsync(
                new DateTime(2026, 10, 10),
                new DateTime(2026, 10, 12));

            Assert.Equal(1m, result);
        }
    }

    [Fact]
    public async Task CalculateWorkingDays_SaturdayToSunday_ReturnsZero()
    {
        var (context, service) = CreateService();
        using (context)
        {
            var result = await service.CalculateWorkingDaysAsync(
                new DateTime(2026, 10, 10),
                new DateTime(2026, 10, 11));

            Assert.Equal(0m, result);
        }
    }

    [Fact]
    public async Task CalculateWorkingDays_MondayToSunday_ReturnsFive()
    {
        var (context, service) = CreateService();
        using (context)
        {
            var result = await service.CalculateWorkingDaysAsync(
                new DateTime(2026, 10, 12),
                new DateTime(2026, 10, 18));

            Assert.Equal(5m, result);
        }
    }

    [Fact]
    public async Task CalculateWorkingDays_SameWeekday_ReturnsOne()
    {
        var (context, service) = CreateService();
        using (context)
        {
            var result = await service.CalculateWorkingDaysAsync(
                new DateTime(2026, 10, 13),
                new DateTime(2026, 10, 13));

            Assert.Equal(1m, result);
        }
    }

    [Fact]
    public async Task CalculateWorkingDays_FridayToMonday_ReturnsTwo()
    {
        var (context, service) = CreateService();
        using (context)
        {
            var result = await service.CalculateWorkingDaysAsync(
                new DateTime(2026, 10, 16),
                new DateTime(2026, 10, 19));

            Assert.Equal(2m, result);
        }
    }

    [Fact]
    public async Task CalculateWorkingDays_EndDateBeforeStartDate_ThrowsArgumentException()
    {
        var (context, service) = CreateService();
        using (context)
        {
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await service.CalculateWorkingDaysAsync(
                    new DateTime(2026, 10, 14),
                    new DateTime(2026, 10, 12)));

            Assert.Equal(
                "End date cannot be earlier than start date.",
                exception.Message);
        }
    }

    [Fact]
    public async Task CalculateWorkingDays_WithTimeComponents_CalculatesByDateOnly()
    {
        var (context, service) = CreateService();
        using (context)
        {
            var result = await service.CalculateWorkingDaysAsync(
                new DateTime(2026, 10, 12, 15, 30, 0),
                new DateTime(2026, 10, 13, 9, 0, 0));

            Assert.Equal(2m, result);
        }
    }

    [Fact]
    public async Task CalculateWorkingDays_PublicHolidayOnWeekday_ExcludesHoliday()
    {
        var (context, service) = CreateService();
        using (context)
        {
            context.PublicHolidays.Add(new PublicHoliday
            {
                Name = "Test Public Holiday",
                Date = new DateTime(2026, 10, 13)
            });

            await context.SaveChangesAsync();

            var result = await service.CalculateWorkingDaysAsync(
                new DateTime(2026, 10, 12),
                new DateTime(2026, 10, 14));

            // Monday to Wednesday has 3 weekdays, but Tuesday is a holiday.
            Assert.Equal(2m, result);
        }
    }
}
