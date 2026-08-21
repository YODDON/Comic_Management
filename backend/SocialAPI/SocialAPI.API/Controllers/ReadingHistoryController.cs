using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using SocialAPI.DTOs;
using SocialAPI.Application.Features.ReadingHistories.Commands;
using SocialAPI.Application.Features.ReadingHistories.Queries;

namespace SocialAPI.API.Controllers;

[ApiController]
[Route("api/reading-history")]
[Authorize(Roles = "Admin,Reader")]
public class ReadingHistoryController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReadingHistoryController(IMediator mediator) =>
        _mediator = mediator;

    [HttpGet("me")]
    public async Task<IActionResult> GetMine([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        if (!TryGetUserIds(out _, out var userId)) return Unauthorized();
        return Ok(await _mediator.Send(new GetMineQuery { UserId = userId, Page = page, PageSize = pageSize }));
    }

    [HttpPost]
    public async Task<IActionResult> Record([FromBody] RecordReadingHistoryDto request)
    {
        if (!TryGetUserIds(out var numericUserId, out var userId)) return Unauthorized();
        if (request.ComicId == Guid.Empty || request.ChapterId == Guid.Empty)
            return BadRequest(new { message = "ComicId và ChapterId là bắt buộc." });
        return Ok(await _mediator.Send(new RecordCommand { NumericUserId = numericUserId, UserId = userId, Request = request }));
    }

    private bool TryGetUserIds(out int numericUserId, out Guid userId)
    {
        numericUserId = 0;
        userId = Guid.Empty;
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(value, out numericUserId)) return false;
        userId = new Guid(System.Security.Cryptography.MD5.HashData(BitConverter.GetBytes(numericUserId)));
        return true;
    }
}
