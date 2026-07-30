using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialAPI.Interfaces;

namespace SocialAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Reader")]
    public class FollowsController : ControllerBase
    {
        private readonly IFollowService _followService;

        public FollowsController(IFollowService followService)
        {
            _followService = followService;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyFollows([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { message = "Unauthorized" });
            }

            var response = await _followService.GetUserFollowsAsync(userId, page, pageSize);
            return StatusCode(response.StatusCode, response);
        }

        [HttpPost("{followingId}")]
        public async Task<IActionResult> AddFollow(Guid followingId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { message = "Unauthorized" });
            }

            var response = await _followService.AddFollowAsync(userId, followingId);
            return StatusCode(response.StatusCode, response);
        }

        [HttpDelete("{followingId}")]
        public async Task<IActionResult> RemoveFollow(Guid followingId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { message = "Unauthorized" });
            }

            var response = await _followService.RemoveFollowAsync(userId, followingId);
            return StatusCode(response.StatusCode, response);
        }
    }
}
