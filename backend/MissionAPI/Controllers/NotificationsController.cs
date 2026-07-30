using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MissionAPI.DTOs;
using MissionAPI.Interfaces;

namespace MissionAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Reader")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] bool? isRead, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { Message = "Invalid token." });
            }

            var result = await _notificationService.GetNotificationsAsync(userId, isRead, pageIndex, pageSize);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationDto request)
        {
            var result = await _notificationService.CreateNotificationAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllForAdmin()
        {
            var result = await _notificationService.GetAllNotificationsForAdminAsync();
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("admin/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateForAdmin(Guid id, [FromBody] UpdateNotificationDto request)
        {
            var result = await _notificationService.UpdateNotificationAsync(id, request);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("admin/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteForAdmin(Guid id)
        {
            var result = await _notificationService.DeleteNotificationForAdminAsync(id);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { Message = "Invalid token." });
            }

            var result = await _notificationService.MarkAsReadAsync(userId, id);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(Guid id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { Message = "Invalid token." });
            }

            var result = await _notificationService.DeleteNotificationAsync(userId, id);
            return StatusCode(result.StatusCode, result);
        }
    }
}
