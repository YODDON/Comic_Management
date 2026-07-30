using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialAPI.DTOs;
using SocialAPI.Interfaces;
using SharedKernel.Enums;

namespace SocialAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommentsController : ControllerBase
    {
        private readonly ICommentService _commentService;
        private readonly IMissionProgressNotifier _missionProgressNotifier;

        public CommentsController(
            ICommentService commentService,
            IMissionProgressNotifier missionProgressNotifier)
        {
            _commentService = commentService;
            _missionProgressNotifier = missionProgressNotifier;
        }

        [HttpGet]
        public async Task<IActionResult> GetComments([FromQuery] Guid? comicId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (comicId == null || comicId == Guid.Empty)
            {
                return BadRequest("ComicId is required and must be a valid GUID.");
            }

            var result = await _commentService.GetCommentsAsync(comicId.Value, page, pageSize);
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Reader")]
        public async Task<IActionResult> CreateComment([FromBody] CreateCommentDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out var numericUserId))
            {
                return Unauthorized();
            }
            var userId = ToSocialUserId(numericUserId);

            var result = await _commentService.CreateCommentAsync(userId, dto);
            if (result == null)
            {
                return NotFound("Comic not found");
            }

            await _missionProgressNotifier.RecordAsync(
                numericUserId, MissionType.LeaveComment, result.Id, result.CreatedAt);

            return CreatedAtAction(nameof(GetComments), new { comicId = result.ComicId }, result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Reader")]
        public async Task<IActionResult> DeleteComment(Guid id)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out var numericUserId))
            {
                return Unauthorized();
            }
            var userId = ToSocialUserId(numericUserId);

            var isAdmin = User.IsInRole("Admin");

            try
            {
                var success = await _commentService.DeleteCommentAsync(id, userId, isAdmin);
                if (!success)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }
        private static Guid ToSocialUserId(int userId) =>
            new(System.Security.Cryptography.MD5.HashData(System.BitConverter.GetBytes(userId)));
    }
}
