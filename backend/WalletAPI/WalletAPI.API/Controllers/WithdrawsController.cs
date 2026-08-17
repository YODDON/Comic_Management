using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WalletAPI.DTOs;
using WalletAPI.Interfaces;
using MediatR;
using WalletAPI.Application.Features.Currency.Queries;
using WalletAPI.Application.Features.Wallet.Commands;
using WalletAPI.Application.Features.Withdraw.Commands;
using WalletAPI.Application.Features.Withdraw.Queries;
using SharedKernel.Enums;

namespace WalletAPI.Controllers;

[ApiController]
[Route("withdraws")]
[Authorize(Roles = "Admin,Reader")]
public class WithdrawsController : ControllerBase
{
    private readonly IMediator _mediator;

    public WithdrawsController(IMediator mediator)
    {
        _mediator = mediator;
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

        var response = await _mediator.Send(new CreateWithdrawCommand { UserId = userId, Request = request });
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

        var response = await _mediator.Send(new GetWithdrawableQuery { UserId = userId });
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

        var response = await _mediator.Send(new GetMyWithdrawsQuery { UserId = userId, PageNumber = pageNumber, PageSize = pageSize });
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
        var response = await _mediator.Send(new GetAdminWithdrawsQuery { Status = status, Search = search, PageNumber = pageNumber, PageSize = pageSize });
        return StatusCode(response.StatusCode, response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateWithdrawStatusRequestDto request)
    {
        var response = await _mediator.Send(new UpdateWithdrawStatusCommand { Id = id, Request = request });
        return StatusCode(response.StatusCode, response);
    }
}
