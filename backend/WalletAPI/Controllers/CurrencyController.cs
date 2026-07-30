using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WalletAPI.Interfaces;
using WalletAPI.DTOs;

namespace WalletAPI.Controllers;

[ApiController]
[Route("currency")]
[Authorize(Roles = "Admin,Reader")]
public class CurrencyController : ControllerBase
{
    private readonly ICurrencyService _currencyService;

    public CurrencyController(ICurrencyService currencyService)
    {
        _currencyService = currencyService;
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

        var response = await _currencyService.GetHistoryAsync(userId, pageNumber, pageSize);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateEntry([FromBody] CreateCurrencyEntryRequestDto request)
    {
        var response = await _currencyService.CreateEntryAsync(request);
        return StatusCode(response.StatusCode, response);
    }
}
