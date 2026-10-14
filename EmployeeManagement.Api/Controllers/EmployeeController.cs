using EmployeeManagement.Api.DTOs;
using EmployeeManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EmployeeManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeeController : ControllerBase
{
    private readonly EmployeeService _employeeService;

    public EmployeeController(EmployeeService employeeService)
    {
        _employeeService = employeeService;
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
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetEmployees()
    {
        var employees = await _employeeService.GetAllAsync();
        return Ok(employees);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetEmployee(int id)
    {
        // Admin can view any employee.
        if (!User.IsInRole("Admin"))
        {
            var employeeIdClaim = User.FindFirstValue("EmployeeId");

            if (!int.TryParse(employeeIdClaim, out var employeeId)
                || employeeId != id)
            {
                return Forbid();
            }
        }

        var employee = await _employeeService.GetByIdAsync(id);

        if (employee == null)
        {
            return NotFound();
        }

        return Ok(employee);
    }

    [HttpGet("search")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Search(
        [FromQuery] string? search,
        [FromQuery] int? departmentId,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (departmentId.HasValue && departmentId.Value <= 0)
        {
            return BadRequest(new
            {
                message = "DepartmentId must be greater than zero."
            });
        }

        var result = await _employeeService.SearchAsync(
            search,
            departmentId,
            isActive,
            page,
            pageSize);

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateEmployee(
        CreateEmployeeRequest request)
    {
        var result = await _employeeService.CreateAsync(
            request,
            GetActorUserId(),
            GetActorUsername());

        return CreatedAtAction(
            nameof(GetEmployee),
            new { id = result.Id },
            result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateEmployee(
        int id,
        UpdateEmployeeRequest request)
    {
        var employee = await _employeeService.UpdateAsync(
            id,
            request,
            GetActorUserId(),
            GetActorUsername());

        if (employee == null)
        {
            return NotFound();
        }

        return Ok(employee);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteEmployee(int id)
    {
        var deleted = await _employeeService.DeleteAsync(
            id,
            GetActorUserId(),
            GetActorUsername());

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
