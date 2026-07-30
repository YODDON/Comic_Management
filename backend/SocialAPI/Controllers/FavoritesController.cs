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
    public class FavoritesController : ControllerBase
    {
        private readonly IFavoriteService _favoriteService;

        public FavoritesController(IFavoriteService favoriteService)
        {
            _favoriteService = favoriteService;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyFavorites([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { message = "Unauthorized" });
            }

            var response = await _favoriteService.GetUserFavoritesAsync(userId, page, pageSize);
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

            var response = await _favoriteService.AddFavoriteAsync(userId, comicId);
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

            var response = await _favoriteService.RemoveFavoriteAsync(userId, comicId);
            return StatusCode(response.StatusCode, response);
        }
    }
}
