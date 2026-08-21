using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using SocialAPI.Application.Features.Follows.Commands;
using SocialAPI.Application.Features.Follows.Queries;

namespace SocialAPI.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Reader")]
    public class FollowsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public FollowsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyFollows([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { message = "Unauthorized" });
            }

            var response = await _mediator.Send(new GetUserFollowsQuery { UserId = userId, Page = page, PageSize = pageSize });
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

            var response = await _mediator.Send(new AddFollowCommand { UserId = userId, FollowingId = followingId });
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

            var response = await _mediator.Send(new RemoveFollowCommand { UserId = userId, FollowingId = followingId });
            return StatusCode(response.StatusCode, response);
        }
    }
}
