using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserAPI.Application.DTOs;
using UserAPI.Application.Interfaces; using UserAPI.Domain.Interfaces;

namespace UserAPI.API.Controllers;

[ApiController]
[Route("auth/admin/users")]
[Authorize(Roles = "Admin")]
public class AdminUsersController : ControllerBase
{
    private readonly IAdminUserService _adminUserService;

    public AdminUsersController(IAdminUserService adminUserService) => _adminUserService = adminUserService;

    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var response = await _adminUserService.GetUsersAsync(search, isActive, pageNumber, pageSize);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var response = await _adminUserService.GetUserAsync(id);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPut("{id:int}/lock")]
    public async Task<IActionResult> UpdateLock(int id, [FromBody] UpdateUserLockDto request)
    {
        var response = await _adminUserService.UpdateLockAsync(id, GetCurrentUserId(), request);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPut("{id:int}/role")]
    public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateUserRoleDto request)
    {
        var response = await _adminUserService.UpdateRoleAsync(id, GetCurrentUserId(), request);
        return StatusCode(response.StatusCode, response);
    }

    private int? GetCurrentUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
