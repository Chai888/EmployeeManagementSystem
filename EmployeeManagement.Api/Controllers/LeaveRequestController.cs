using EmployeeManagement.Api.DTOs;
using EmployeeManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EmployeeManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeaveRequestController : ControllerBase
{
    private readonly LeaveRequestService _service;
    private readonly LeaveCalculationService _leaveCalculationService;

    public LeaveRequestController(
    LeaveRequestService service,
    LeaveCalculationService leaveCalculationService)
    {
        _service = service;
        _leaveCalculationService = leaveCalculationService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status)
    {
        var result = await _service.GetAllAsync(status);

        return Ok(result);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> Get(int id)
    {
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var isAdmin = User.IsInRole("Admin");

        var result = await _service.GetByIdAsync(id);

        if (result == null)
        {
            return NotFound();
        }

        if (!isAdmin)
        {
            var isOwner = await _service.IsOwnerAsync(id, userId);

            if (!isOwner)
            {
                return Forbid();
            }
        }

        return Ok(result);
    }

    [HttpGet("my")]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> GetMyLeaveRequests()
    {
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var result = await _service.GetMyLeaveRequestsAsync(userId);

        return Ok(result);
    }

    [HttpGet("working-days")]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> CalculateWorkingDays(
    [FromQuery] DateTime startDate,
    [FromQuery] DateTime endDate)
    {
        if (startDate == default || endDate == default)
        {
            return BadRequest("Start date and end date are required.");
        }

        if (endDate.Date < startDate.Date)
        {
            return BadRequest(
                "End date cannot be earlier than start date.");
        }

        if (startDate.Year != endDate.Year)
        {
            return BadRequest(
                "Leave requests cannot cross calendar years.");
        }

        try
        {
            var workingDays =
                await _leaveCalculationService.CalculateWorkingDaysAsync(
                    startDate.Date,
                    endDate.Date);

            return Ok(workingDays);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> Create(
        CreateLeaveRequest request)
    {
        try
        {
            var userIdClaim = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var result = await _service.CreateMyAsync(
                userId,
                request);

            return CreatedAtAction(
                nameof(Get),
                new { id = result.Id },
                result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }


    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        UpdateLeaveStatusRequest request)
    {
        try
        {
            var userIdClaim = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var username =
                User.FindFirstValue(ClaimTypes.Name) ??
                User.FindFirstValue("username") ??
                User.FindFirstValue("unique_name") ??
                userId.ToString();

            var result = await _service.UpdateStatusAsync(
                id,
                request.Status,
                userId,
                username);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}