using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WalletAPI.Interfaces;
using MediatR;
using WalletAPI.Application.Features.Currency.Queries;
using WalletAPI.Application.Features.Wallet.Commands;
using WalletAPI.Application.Features.Withdraw.Commands;
using WalletAPI.Application.Features.Withdraw.Queries;
using WalletAPI.DTOs;

namespace WalletAPI.Controllers;

[ApiController]
[Route("currency")]
[Authorize(Roles = "Admin,Reader")]
public class CurrencyController : ControllerBase
{
    private readonly IMediator _mediator;

    public CurrencyController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Invalid token." });
        }

        var response = await _mediator.Send(new GetHistoryQuery { UserId = userId, PageNumber = pageNumber, PageSize = pageSize });
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateEntry([FromBody] CreateCurrencyEntryRequestDto request)
    {
        var response = await _mediator.Send(new WalletAPI.Application.Features.Currency.Commands.CreateCurrencyEntryCommand { Request = request });
        return StatusCode(response.StatusCode, response);
    }
}
