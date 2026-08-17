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
    public class MissionsController : ControllerBase
    {
        private readonly IMissionService _missionService;

        public MissionsController(IMissionService missionService)
        {
            _missionService = missionService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Reader")]
        public async Task<IActionResult> GetAllMissions()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null || !int.TryParse(claim.Value, out var userId)) return Unauthorized();
            var result = await _missionService.GetUserMissionsAsync(userId);
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

            var result = await _missionService.GetUserMissionsAsync(userId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllMissionsForAdmin()
        {
            var result = await _missionService.GetAllMissionsForAdminAsync();
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateMission([FromBody] CreateMissionDto request)
        {
            var result = await _missionService.CreateMissionAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateMission(Guid id, [FromBody] UpdateMissionDto request)
        {
            var result = await _missionService.UpdateMissionAsync(id, request);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteMission(Guid id)
        {
            var result = await _missionService.DeleteMissionAsync(id);
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

            var result = await _missionService.CompleteMissionStepAsync(userId, id);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("lobby-heartbeat")]
        [Authorize(Roles = "Admin,Reader")]
        public async Task<IActionResult> TrackLobbyMinute()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null || !int.TryParse(claim.Value, out var userId)) return Unauthorized();
            var result = await _missionService.TrackLobbyMinuteAsync(userId);
            return StatusCode(result.StatusCode, result);
        }
    }
}
