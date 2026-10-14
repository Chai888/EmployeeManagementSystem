using EmployeeManagement.Api.DTOs;
using EmployeeManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EmployeeManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PublicHolidayController : ControllerBase
{
    private readonly PublicHolidayService _publicHolidayService;

    public PublicHolidayController(
        PublicHolidayService publicHolidayService)
    {
        _publicHolidayService = publicHolidayService;
    }

    private int? GetActorUserId()
    {
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return int.TryParse(userIdClaim, out var userId)
            ? userId
            : null;
    }

    private string GetActorUsername()
    {
        return User.FindFirstValue(ClaimTypes.Name)
            ?? User.FindFirstValue("username")
            ?? User.FindFirstValue("unique_name")
            ?? User.FindFirstValue(ClaimTypes.Email)
            ?? "Unknown";
    }

    [HttpGet]
    public async Task<IActionResult> GetPublicHolidays(
        [FromQuery] int? year = null)
    {
        if (year.HasValue && (year.Value < 1900 || year.Value > 9998))
        {
            return BadRequest(new
            {
                message = "Year must be between 1900 and 9998."
            });
        }

        var holidays = await _publicHolidayService.GetAllAsync(year);

        return Ok(holidays);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetPublicHoliday(int id)
    {
        var holiday = await _publicHolidayService.GetByIdAsync(id);

        if (holiday == null)
        {
            return NotFound(new
            {
                message = "Public holiday not found."
            });
        }

        return Ok(holiday);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreatePublicHoliday(
        [FromBody] PublicHolidayRequest request)
    {
        try
        {
            var result = await _publicHolidayService.CreateAsync(
                request,
                GetActorUserId(),
                GetActorUsername());

            return CreatedAtAction(
                nameof(GetPublicHoliday),
                new { id = result.Id },
                result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdatePublicHoliday(
        int id,
        [FromBody] PublicHolidayRequest request)
    {
        try
        {
            var result = await _publicHolidayService.UpdateAsync(
                id,
                request,
                GetActorUserId(),
                GetActorUsername());

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeletePublicHoliday(int id)
    {
        try
        {
            await _publicHolidayService.DeleteAsync(
                id,
                GetActorUserId(),
                GetActorUsername());

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}