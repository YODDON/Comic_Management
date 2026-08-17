using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MissionAPI.DTOs;
using MediatR;
using MissionAPI.Application.Features.Missions.Queries;
using MissionAPI.Application.Features.Missions.Commands;
using MissionAPI.Application.Features.Notifications.Queries;
using MissionAPI.Application.Features.Notifications.Commands;

namespace MissionAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Reader")]
    public class NotificationsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public NotificationsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] bool? isRead, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { Message = "Invalid token." });
            }

            var result = await _mediator.Send(new GetNotificationsQuery { UserId = userId, IsRead = isRead, PageIndex = pageIndex, PageSize = pageSize });
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationDto request)
        {
            var result = await _mediator.Send(new CreateNotificationCommand { Request = request });
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllForAdmin()
        {
            var result = await _mediator.Send(new GetAllNotificationsForAdminQuery());
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("admin/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateForAdmin(Guid id, [FromBody] UpdateNotificationDto request)
        {
            var result = await _mediator.Send(new UpdateNotificationCommand { Id = id, Request = request });
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("admin/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteForAdmin(Guid id)
        {
            var result = await _mediator.Send(new DeleteNotificationForAdminCommand { Id = id });
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

            var result = await _mediator.Send(new MarkAsReadCommand { UserId = userId, Id = id });
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

            var result = await _mediator.Send(new DeleteNotificationCommand { UserId = userId, Id = id });
            return StatusCode(result.StatusCode, result);
        }
    }
}
