
using EmployeeManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UserController : ControllerBase
{
    private readonly UserService _service;

    public UserController(UserService service)
    {
        _service = service;
    }

    [HttpGet("employee-accounts")]
    public async Task<IActionResult> GetEmployeeAccounts()
    {
        return Ok(await _service.GetEmployeeAccountsAsync());
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> SetAccountStatus(
        int id,
        [FromQuery] bool isActive)
    {
        await _service.SetAccountStatusAsync(id, isActive);

        return NoContent();
    }
}