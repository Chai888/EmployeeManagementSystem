using EmployeeManagement.Api.DTOs;
using EmployeeManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EmployeeManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DepartmentController : ControllerBase
{
    private readonly DepartmentService _departmentService;

    public DepartmentController(
        DepartmentService departmentService)
    {
        _departmentService = departmentService;
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
    public async Task<IActionResult> GetDepartments()
    {
        var departments = await _departmentService.GetAllAsync();
        return Ok(departments);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDepartment(int id)
    {
        var department = await _departmentService.GetByIdAsync(id);

        if (department == null)
        {
            return NotFound();
        }

        return Ok(department);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateDepartment(
        [FromBody] CreateDepartmentRequest request)
    {
        try
        {
            var result = await _departmentService.CreateAsync(
                request,
                GetActorUserId(),
                GetActorUsername());

            return CreatedAtAction(
                nameof(GetDepartment),
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
    public async Task<IActionResult> UpdateDepartment(
        int id,
        [FromBody] CreateDepartmentRequest request)
    {
        try
        {
            var result = await _departmentService.UpdateAsync(
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
    public async Task<IActionResult> DeleteDepartment(int id)
    {
        try
        {
            await _departmentService.DeleteAsync(
                id,
                GetActorUserId(),
                GetActorUsername());

            return NoContent();
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
}
