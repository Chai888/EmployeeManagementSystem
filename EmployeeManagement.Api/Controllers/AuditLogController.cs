
using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AuditLogController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AuditLogController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? action = null,
        [FromQuery] string? entityType = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.AuditLogs
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(x => x.Action.Contains(action));
        }

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(x =>
                    x.EntityType == "Leave" &&
                    (x.Action == "Approved" || x.Action == "Rejected"));
        }

        query = query.OrderByDescending(x => x.CreatedAtUtc);

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AuditLogResponse
            {
                Id = x.Id,
                UserId = x.UserId,
                Username = x.Username,
                Action = x.Action,
                EntityType = x.EntityType,
                EntityId = x.EntityId,
                Details = x.Details,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync();

        return Ok(new
        {
            page,
            pageSize,
            totalCount,
            totalPages = (int)Math.Ceiling(
                totalCount / (double)pageSize),
            items
        });
    }
}