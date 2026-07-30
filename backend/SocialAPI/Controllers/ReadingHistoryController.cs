using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocialAPI.Data;
using SocialAPI.DTOs;
using SocialAPI.Entities;
using SocialAPI.Interfaces;
using SharedKernel.Enums;

namespace SocialAPI.Controllers
{
    [ApiController]
    [Route("api/reading-history")]
    [Authorize(Roles = "Admin,Reader")]
    public class ReadingHistoryController : ControllerBase
    {
        private readonly SocialDbContext _context;
        private readonly IMissionProgressNotifier _missionProgressNotifier;

        public ReadingHistoryController(
            SocialDbContext context,
            IMissionProgressNotifier missionProgressNotifier)
        {
            _context = context;
            _missionProgressNotifier = missionProgressNotifier;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMine([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _context.ReadingHistories.Where(item => item.UserId == userId);
            var total = await query.CountAsync();
            var items = await query.OrderByDescending(item => item.ReadAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(item => new ReadingHistoryDto
                {
                    Id = item.Id,
                    ComicId = item.ComicId,
                    ChapterId = item.ChapterId,
                    ReadAt = item.ReadAt
                }).ToListAsync();

            return Ok(new { items, total, page, pageSize });
        }

        [HttpPost]
        public async Task<IActionResult> Record([FromBody] RecordReadingHistoryDto request)
        {
            if (!TryGetUserIds(out var numericUserId, out var userId)) return Unauthorized();
            if (request.ComicId == Guid.Empty || request.ChapterId == Guid.Empty)
                return BadRequest(new { message = "ComicId và ChapterId là bắt buộc." });

            var item = await _context.ReadingHistories
                .FirstOrDefaultAsync(history => history.UserId == userId && history.ComicId == request.ComicId);

            if (item == null)
            {
                item = new ReadingHistory { UserId = userId, ComicId = request.ComicId };
                _context.ReadingHistories.Add(item);
            }

            item.ChapterId = request.ChapterId;
            item.ReadAt = DateTime.UtcNow;
            item.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _missionProgressNotifier.RecordAsync(
                numericUserId, MissionType.ReadChapter, request.ChapterId, item.ReadAt);

            return Ok(new ReadingHistoryDto { Id = item.Id, ComicId = item.ComicId, ChapterId = item.ChapterId, ReadAt = item.ReadAt });
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

        private bool TryGetUserId(out Guid userId)
        {
            userId = Guid.Empty;
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(value, out var numericUserId)) return false;
            userId = new Guid(System.Security.Cryptography.MD5.HashData(BitConverter.GetBytes(numericUserId)));
            return true;
        }
    }
}
