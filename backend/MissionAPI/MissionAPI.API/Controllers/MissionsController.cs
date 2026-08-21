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
    public class MissionsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public MissionsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Reader")]
        public async Task<IActionResult> GetAllMissions()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null || !int.TryParse(claim.Value, out var userId)) return Unauthorized();
            var result = await _mediator.Send(new GetUserMissionsQuery { UserId = userId });
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("my-missions")]
        [Authorize(Roles = "Admin,Reader")]
        public async Task<IActionResult> GetMyMissions()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { Message = "Invalid token." });
            }

            var result = await _mediator.Send(new GetUserMissionsQuery { UserId = userId });
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllMissionsForAdmin()
        {
            var result = await _mediator.Send(new GetAllMissionsForAdminQuery());
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateMission([FromBody] CreateMissionDto request)
        {
            var result = await _mediator.Send(new CreateMissionCommand { Request = request });
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateMission(Guid id, [FromBody] UpdateMissionDto request)
        {
            var result = await _mediator.Send(new UpdateMissionCommand { Id = id, Request = request });
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteMission(Guid id)
        {
            var result = await _mediator.Send(new DeleteMissionCommand { Id = id });
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("{id}/complete")]
        [Authorize(Roles = "Admin,Reader")]
        public async Task<IActionResult> CompleteMissionStep(Guid id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { Message = "Invalid token." });
            }

            var result = await _mediator.Send(new CompleteMissionStepCommand { UserId = userId, MissionId = id });
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("lobby-heartbeat")]
        [Authorize(Roles = "Admin,Reader")]
        public async Task<IActionResult> TrackLobbyMinute()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null || !int.TryParse(claim.Value, out var userId)) return Unauthorized();
            var result = await _mediator.Send(new TrackLobbyMinuteQuery { UserId = userId });
            return StatusCode(result.StatusCode, result);
        }
    }
}
