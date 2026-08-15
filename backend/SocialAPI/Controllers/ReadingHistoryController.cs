using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialAPI.DTOs;
using SocialAPI.Interfaces;

namespace SocialAPI.Controllers;

[ApiController]
[Route("api/reading-history")]
[Authorize(Roles = "Admin,Reader")]
public class ReadingHistoryController : ControllerBase
{
    private readonly IReadingHistoryService _readingHistoryService;

    public ReadingHistoryController(IReadingHistoryService readingHistoryService) =>
        _readingHistoryService = readingHistoryService;

    [HttpGet("me")]
    public async Task<IActionResult> GetMine([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        if (!TryGetUserIds(out _, out var userId)) return Unauthorized();
        return Ok(await _readingHistoryService.GetMineAsync(userId, page, pageSize));
    }

    [HttpPost]
    public async Task<IActionResult> Record([FromBody] RecordReadingHistoryDto request)
    {
        if (!TryGetUserIds(out var numericUserId, out var userId)) return Unauthorized();
        if (request.ComicId == Guid.Empty || request.ChapterId == Guid.Empty)
            return BadRequest(new { message = "ComicId và ChapterId là bắt buộc." });
        return Ok(await _readingHistoryService.RecordAsync(numericUserId, userId, request));
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
