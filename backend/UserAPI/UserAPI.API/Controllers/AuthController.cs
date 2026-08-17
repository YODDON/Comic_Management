using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using UserAPI.Application.DTOs;
using UserAPI.Application.Features.Auth.Commands;
using UserAPI.Application.Features.Auth.Queries;

namespace UserAPI.API.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            var userAgent = Request.Headers["User-Agent"].ToString();
            var result = await _mediator.Send(new RegisterCommand { Request = request, UserAgent = userAgent });
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var userAgent = Request.Headers["User-Agent"].ToString();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var result = await _mediator.Send(new LoginCommand { Request = request, UserAgent = userAgent, IpAddress = ipAddress });
            return StatusCode(result.StatusCode, result);
        }

        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetProfile()
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            {
                return Unauthorized();
            }

            var result = await _mediator.Send(new GetProfileQuery { UserId = userId });
            return StatusCode(result.StatusCode, result);
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,Reader")]
        [HttpPut("me")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDto request)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _mediator.Send(new UpdateProfileCommand { UserId = userId, Request = request });
            return StatusCode(result.StatusCode, result);
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,Reader")]
        [HttpPut("me/password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _mediator.Send(new ChangePasswordCommand { UserId = userId, Request = request });
            return StatusCode(result.StatusCode, result);
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,Reader")]
        [HttpPost("me/avatar")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateAvatar([FromForm] UpdateAvatarRequestDto request)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _mediator.Send(new UpdateAvatarCommand { UserId = userId, Avatar = request.Avatar });
            return StatusCode(result.StatusCode, result);
        }

        private bool TryGetUserId(out int userId)
        {
            var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(value, out userId);
        }

        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequestDto request)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            {
                return Unauthorized();
            }

            var result = await _mediator.Send(new LogoutCommand { UserId = userId, Request = request });
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("refetchToken")]
        public async Task<IActionResult> RefetchToken([FromBody] RefreshTokenRequestDto request)
        {
            var userAgent = Request.Headers["User-Agent"].ToString();
            var result = await _mediator.Send(new RefreshTokenCommand { Request = request, UserAgent = userAgent });
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
        {
            var result = await _mediator.Send(new ForgotPasswordCommand { Request = request });
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("reset-password")]
        public async Task<IActionResult> CheckResetPasswordToken([FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token)) return BadRequest(SharedKernel.Responses.ApiResponse<string>.ErrorResponse("Token is required.", 400));
            
            var result = await _mediator.Send(new CheckResetPasswordTokenQuery { Token = token });
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
        {
            var result = await _mediator.Send(new ResetPasswordCommand { Request = request });
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("verify-email")]
        public async Task<IActionResult> VerifyEmailGet([FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token)) return BadRequest(SharedKernel.Responses.ApiResponse<string>.ErrorResponse("Token is required.", 400));
            var result = await _mediator.Send(new VerifyEmailCommand { Token = token });
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("verify")]
        public async Task<IActionResult> VerifyEmailPost([FromBody] VerifyEmailRequestDto request)
        {
            var result = await _mediator.Send(new VerifyEmailCommand { Token = request.Token });
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("resend-confirm")]
        public async Task<IActionResult> ResendConfirm([FromBody] ResendConfirmRequestDto request)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var result = await _mediator.Send(new ResendConfirmEmailCommand { Request = request, IpAddress = ipAddress });
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("google")]
        public async Task<IActionResult> LoginByGoogle([FromBody] GoogleLoginRequestDto request)
        {
            var userAgent = Request.Headers["User-Agent"].ToString();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var result = await _mediator.Send(new LoginByGoogleCommand { Request = request, UserAgent = userAgent, IpAddress = ipAddress });
            return StatusCode(result.StatusCode, result);
        }
    }
}
