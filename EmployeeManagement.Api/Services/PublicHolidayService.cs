using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs;
using EmployeeManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Api.Services;

public class PublicHolidayService
{
    private readonly ApplicationDbContext _context;
    private readonly AuditLogService _auditLogService;

    public PublicHolidayService(
        ApplicationDbContext context,
        AuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
    }

    public async Task<List<PublicHolidayResponse>> GetAllAsync(
        int? year = null)
    {
        var query = _context.PublicHolidays
            .AsNoTracking()
            .AsQueryable();

        if (year.HasValue)
        {
            query = query.Where(x => x.Date.Year == year.Value);
        }

        return await query
            .OrderBy(x => x.Date)
            .ThenBy(x => x.Name)
            .Select(x => new PublicHolidayResponse
            {
                Id = x.Id,
                Name = x.Name,
                Date = x.Date,
                Description = x.Description
            })
            .ToListAsync();
    }

    public async Task<PublicHolidayResponse?> GetByIdAsync(int id)
    {
        return await _context.PublicHolidays
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new PublicHolidayResponse
            {
                Id = x.Id,
                Name = x.Name,
                Date = x.Date,
                Description = x.Description
            })
            .FirstOrDefaultAsync();
    }

    public async Task<PublicHolidayResponse> CreateAsync(
        PublicHolidayRequest request,
        int? actorUserId,
        string actorUsername)
    {
        var name = request.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Public holiday name is required.");
        }

        var date = request.Date.Date;

        if (date == DateTime.MinValue.Date)
        {
            throw new ArgumentException(
                "Public holiday date is required.");
        }

        var exists = await _context.PublicHolidays
            .AnyAsync(x => x.Date == date);

        if (exists)
        {
            throw new InvalidOperationException(
                "A public holiday already exists on this date.");
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        var holiday = new PublicHoliday
        {
            Name = name,
            Date = date,
            Description = request.Description?.Trim()
        };

        _context.PublicHolidays.Add(holiday);

        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            actorUserId,
            actorUsername,
            "PublicHolidayCreated",
            "PublicHoliday",
            holiday.Id.ToString(),
            $"Created public holiday. Name: {holiday.Name}; " +
            $"Date: {holiday.Date:yyyy-MM-dd}");

        await transaction.CommitAsync();

        return new PublicHolidayResponse
        {
            Id = holiday.Id,
            Name = holiday.Name,
            Date = holiday.Date,
            Description = holiday.Description
        };
    }

    public async Task<PublicHolidayResponse> UpdateAsync(
        int id,
        PublicHolidayRequest request,
        int? actorUserId,
        string actorUsername)
    {
        var name = request.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Public holiday name is required.");
        }

        var holiday = await _context.PublicHolidays
            .FirstOrDefaultAsync(x => x.Id == id);

        if (holiday == null)
        {
            throw new KeyNotFoundException(
                "Public holiday not found.");
        }

        var date = request.Date.Date;

        if (date == DateTime.MinValue.Date)
        {
            throw new ArgumentException(
                "Public holiday date is required.");
        }

        var duplicateExists = await _context.PublicHolidays
            .AnyAsync(x => x.Id != id && x.Date == date);

        if (duplicateExists)
        {
            throw new InvalidOperationException(
                "A public holiday already exists on this date.");
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        var oldName = holiday.Name;
        var oldDate = holiday.Date;

        holiday.Name = name;
        holiday.Date = date;
        holiday.Description = request.Description?.Trim();

        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            actorUserId,
            actorUsername,
            "PublicHolidayUpdated",
            "PublicHoliday",
            holiday.Id.ToString(),
            $"Updated public holiday. " +
            $"Old name: {oldName}; Old date: {oldDate:yyyy-MM-dd}; " +
            $"New name: {holiday.Name}; New date: {holiday.Date:yyyy-MM-dd}");

        await transaction.CommitAsync();

        return new PublicHolidayResponse
        {
            Id = holiday.Id,
            Name = holiday.Name,
            Date = holiday.Date,
            Description = holiday.Description
        };
    }

    public async Task DeleteAsync(
        int id,
        int? actorUserId,
        string actorUsername)
    {
        var holiday = await _context.PublicHolidays
            .FirstOrDefaultAsync(x => x.Id == id);

        if (holiday == null)
        {
            throw new KeyNotFoundException(
                "Public holiday not found.");
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        var holidayName = holiday.Name;
        var holidayDate = holiday.Date;

        _context.PublicHolidays.Remove(holiday);

        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            actorUserId,
            actorUsername,
            "PublicHolidayDeleted",
            "PublicHoliday",
            id.ToString(),
            $"Deleted public holiday. Name: {holidayName}; " +
            $"Date: {holidayDate:yyyy-MM-dd}");

        await transaction.CommitAsync();
    }
}