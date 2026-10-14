using EmployeeManagement.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Api.Services;

public class LeaveCalculationService
{
    private readonly ApplicationDbContext _context;

    public LeaveCalculationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<decimal> CalculateWorkingDaysAsync(
        DateTime startDate,
        DateTime endDate)
    {
        startDate = startDate.Date;
        endDate = endDate.Date;

        if (endDate < startDate)
        {
            throw new ArgumentException(
                "End date cannot be earlier than start date.");
        }

        var publicHolidayDates = await _context.PublicHolidays
            .AsNoTracking()
            .Where(x => x.Date >= startDate && x.Date <= endDate)
            .Select(x => x.Date.Date)
            .ToListAsync();

        var publicHolidaySet = publicHolidayDates.ToHashSet();

        decimal totalDays = 0;

        for (var date = startDate;
             date <= endDate;
             date = date.AddDays(1))
        {
            var isWeekend =
                date.DayOfWeek == DayOfWeek.Saturday ||
                date.DayOfWeek == DayOfWeek.Sunday;

            var isPublicHoliday = publicHolidaySet.Contains(date);

            if (!isWeekend && !isPublicHoliday)
            {
                totalDays++;
            }
        }

        return totalDays;
    }
}