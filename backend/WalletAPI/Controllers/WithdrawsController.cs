using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WalletAPI.DTOs;
using WalletAPI.Interfaces;
using SharedKernel.Enums;

namespace WalletAPI.Controllers;

[ApiController]
[Route("withdraws")]
[Authorize(Roles = "Admin,Reader")]
public class WithdrawsController : ControllerBase
{
    private readonly IWithdrawService _withdrawService;

    public WithdrawsController(IWithdrawService withdrawService)
    {
        _withdrawService = withdrawService;
    }

    [HttpPost]
    [Authorize(Roles = "Reader")]
    public async Task<IActionResult> Create([FromBody] CreateWithdrawRequestDto request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Invalid token." });
        }

        var response = await _withdrawService.CreateAsync(userId, request);
        return StatusCode(response.StatusCode, response);
    }

    /// <summary>
    /// How much the current user may withdraw (balance minus locked mission coins).
    /// The withdraw form uses this so a user is never shown a balance they cannot actually cash out.
    /// </summary>
    [HttpGet("withdrawable")]
    public async Task<IActionResult> GetWithdrawable()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Invalid token." });
        }

        var response = await _withdrawService.GetWithdrawableAsync(userId);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMine(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Invalid token." });
        }

        var response = await _withdrawService.GetMineAsync(userId, pageNumber, pageSize);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAdmin(
        [FromQuery] WithdrawStatus? status,
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var response = await _withdrawService.GetAdminAsync(status, search, pageNumber, pageSize);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateWithdrawStatusRequestDto request)
    {
        var response = await _withdrawService.UpdateStatusAsync(id, request);
        return StatusCode(response.StatusCode, response);
    }
}
