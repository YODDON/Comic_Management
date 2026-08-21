using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using SharedKernel.Responses;
using UserAPI.Application.DTOs;
using UserAPI.Application.Interfaces;
using UserAPI.Domain.Entities;
using UserAPI.Domain.Interfaces;
using UserAPI.Application.Services;
using Microsoft.Extensions.Configuration;

namespace UserAPI.Application.Features.Auth.Commands
{
    public class RefreshTokenCommand : IRequest<ApiResponse<AuthResponseDto>>
    {
        public RefreshTokenRequestDto Request { get; set; }
        public string UserAgent { get; set; }
    }

    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, ApiResponse<AuthResponseDto>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IEmailService _emailService;
        private readonly IAvatarStorageService _avatarStorageService;
        private readonly IMemoryCache _memoryCache;
        private readonly string _frontendUrl;

        public RefreshTokenCommandHandler(IUserRepository userRepository, IJwtTokenGenerator jwtTokenGenerator, IEmailService emailService, IAvatarStorageService avatarStorageService, IMemoryCache memoryCache, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _jwtTokenGenerator = jwtTokenGenerator;
            _emailService = emailService;
            _avatarStorageService = avatarStorageService;
            _memoryCache = memoryCache;
            _frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL")
                ?? configuration["FrontendUrl"]
                ?? "http://localhost:5173";
        }

        public async Task<ApiResponse<AuthResponseDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetUserByRefreshTokenAsync(request.Request.RefreshToken);
            if (user == null)
            {
                return ApiResponse<AuthResponseDto>.ErrorResponse("Invalid refresh token.", 401);
            }

            if (!user.IsEmailVerified)
            {
                return ApiResponse<AuthResponseDto>.ErrorResponse("Email chưa được xác nhận.", 403);
            }

            var existingToken = user.RefreshTokens.First(rt => rt.Token == request.Request.RefreshToken);

            if (existingToken.IsRevoked || existingToken.ExpiresAt < DateTime.UtcNow)
            {
                // Security breach detected: Revoke ALL refresh tokens of this user
                foreach (var rt in user.RefreshTokens)
                {
                    rt.IsRevoked = true;
                }
                await _userRepository.SaveChangesAsync();
                return ApiResponse<AuthResponseDto>.ErrorResponse("Invalid refresh token. All sessions revoked.", 401);
            }

            if (existingToken.DeviceInfo != request.UserAgent)
            {
                // Suspicious activity: Revoke ALL refresh tokens
                foreach (var rt in user.RefreshTokens)
                {
                    rt.IsRevoked = true;
                }
                await _userRepository.SaveChangesAsync();
                return ApiResponse<AuthResponseDto>.ErrorResponse("Device mismatch. All sessions revoked.", 401);
            }

            existingToken.IsRevoked = true;

            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
            var newAccessToken = _jwtTokenGenerator.GenerateAccessToken(user, roles);
            var newRefreshTokenString = _jwtTokenGenerator.GenerateRefreshToken();

            var newRefreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = newRefreshTokenString,
                DeviceInfo = request.UserAgent,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            };
            await _userRepository.AddRefreshTokenAsync(newRefreshToken);
            await _userRepository.SaveChangesAsync();

            return new ApiResponse<AuthResponseDto>(
                new AuthResponseDto
                {
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshTokenString,
                    ExpiresIn = 3600
                },
                "Token refreshed successfully.",
                200
            );
        }
        
        private bool IsPasswordStrong(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8) return false;
            var hasUpperChar = new System.Text.RegularExpressions.Regex(@"[A-Z]+");
            var hasNumber = new System.Text.RegularExpressions.Regex(@"[0-9]+");
            var hasSymbols = new System.Text.RegularExpressions.Regex(@"[!@#$%^&*()_+=\[\]{};':""\|,.<>/?]+");
            return hasUpperChar.IsMatch(password) && hasNumber.IsMatch(password) && hasSymbols.IsMatch(password);
        }
        
        private UserProfileDto ToProfileDto(User user)
        {
            return new UserProfileDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                IsEmailVerified = user.IsEmailVerified,
                IsActive = user.IsActive,
                AvatarUrl = user.AvatarUrl,
                CreatedAt = user.CreatedAt,
                Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
            };
        }
    }
}
