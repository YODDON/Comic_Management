using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using SocialAPI.Application.Features.Favorites.Commands;
using SocialAPI.Application.Features.Favorites.Queries;

namespace SocialAPI.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Reader")]
    public class FavoritesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public FavoritesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyFavorites([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { message = "Unauthorized" });
            }

            var response = await _mediator.Send(new GetUserFavoritesQuery { UserId = userId, Page = page, PageSize = pageSize });
            return StatusCode(response.StatusCode, response);
        }

        [HttpPost("{comicId}")]
        public async Task<IActionResult> AddFavorite(Guid comicId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { message = "Unauthorized" });
            }

            var response = await _mediator.Send(new AddFavoriteCommand { UserId = userId, ComicId = comicId });
            return StatusCode(response.StatusCode, response);
        }

        [HttpDelete("{comicId}")]
        public async Task<IActionResult> RemoveFavorite(Guid comicId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { message = "Unauthorized" });
            }

            var response = await _mediator.Send(new RemoveFavoriteCommand { UserId = userId, ComicId = comicId });
            return StatusCode(response.StatusCode, response);
        }
    }
}
