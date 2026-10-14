using System.Security.Claims;
using EmployeeManagement.Api.DTOs;
using EmployeeManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class LeaveEntitlementController : ControllerBase
{
    private readonly LeaveEntitlementService _service;

    public LeaveEntitlementController(
        LeaveEntitlementService service)
    {
        _service = service;
    }

    [HttpGet("employees/{employeeId:int}")]
    public async Task<IActionResult> GetForEmployee(
        int employeeId,
        [FromQuery] int year)
    {
        if (year < 2000 || year > 2100)
        {
            return BadRequest("Invalid leave year.");
        }

        var result = await _service.GetForEmployeeAsync(
            employeeId,
            year);

        return Ok(result);
    }

    [HttpPut("employees/{employeeId:int}")]
    public async Task<IActionResult> Save(
        int employeeId,
        LeaveEntitlementRequest request)
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

            var result = await _service.SaveAsync(
                employeeId,
                request,
                userId,
                username);

            return Ok(result);
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
}
