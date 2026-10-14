using System.Security.Claims;
using EmployeeManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EmployeeManagement.Api.Data;

namespace EmployeeManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeaveBalanceController : ControllerBase
{
    private readonly LeaveBalanceService _service;
    private readonly ApplicationDbContext _context;

    public LeaveBalanceController(
        LeaveBalanceService service,
        ApplicationDbContext context)
    {
        _service = service;
        _context = context;
    }

    [HttpGet("my")]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> GetMyBalance(
        [FromQuery] int? year)
    {
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var employeeId = await _context.Users
            .Where(x => x.Id == userId)
            .Select(x => x.EmployeeId)
            .FirstOrDefaultAsync();

        if (employeeId == null)
        {
            return NotFound("Employee profile not found.");
        }

        var result = await _service.GetByEmployeeAsync(
            employeeId.Value,
            year ?? DateTime.UtcNow.Year);

        if (result == null)
        {
            return NotFound("Leave balance not found.");
        }

        return Ok(result);
    }


    [HttpGet("my/all")]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> GetMyBalances(
        [FromQuery] int? year)
    {
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var employeeId = await _context.Users
            .Where(x => x.Id == userId)
            .Select(x => x.EmployeeId)
            .FirstOrDefaultAsync();

        if (employeeId == null)
        {
            return NotFound("Employee profile not found.");
        }

        var result = await _service.GetAllByEmployeeAsync(
            employeeId.Value,
            year ?? DateTime.UtcNow.Year);

        return Ok(result);
    }

}