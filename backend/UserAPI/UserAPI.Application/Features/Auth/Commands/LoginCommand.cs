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
    public class LoginCommand : IRequest<ApiResponse<AuthResponseDto>>
    {
        public LoginRequestDto Request { get; set; }
        public string UserAgent { get; set; }
        public string IpAddress { get; set; }
    }

    public class LoginCommandHandler : IRequestHandler<LoginCommand, ApiResponse<AuthResponseDto>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IEmailService _emailService;
        private readonly IAvatarStorageService _avatarStorageService;
        private readonly IMemoryCache _memoryCache;
        private readonly string _frontendUrl;

        public LoginCommandHandler(IUserRepository userRepository, IJwtTokenGenerator jwtTokenGenerator, IEmailService emailService, IAvatarStorageService avatarStorageService, IMemoryCache memoryCache, IConfiguration configuration)
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

        public async Task<ApiResponse<AuthResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var normalizedEmail = request.Request.Email.Trim().ToLowerInvariant();
            var cacheKey = $"LoginAttempts_{request.IpAddress}_{normalizedEmail}";
            _memoryCache.TryGetValue(cacheKey, out int failedAttempts);

            if (failedAttempts >= 5)
            {
                return ApiResponse<AuthResponseDto>.ErrorResponse("Too many login attempts. Please try again in 15 minutes.", 429);
            }

            var user = await _userRepository.GetUserByEmailAsync(normalizedEmail);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Request.Password, user.PasswordHash))
            {
                failedAttempts++;
                var cacheOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(15));
                _memoryCache.Set(cacheKey, failedAttempts, cacheOptions);

                return ApiResponse<AuthResponseDto>.ErrorResponse("Invalid email or password.", 401);
            }

            _memoryCache.Remove(cacheKey);

            if (!user.IsActive)
            {
                return ApiResponse<AuthResponseDto>.ErrorResponse("Your account is currently inactive.", 403);
            }

            if (!user.IsEmailVerified)
            {
                return ApiResponse<AuthResponseDto>.ErrorResponse("Email chưa được xác nhận. Vui lòng kiểm tra hộp thư trước khi đăng nhập.", 403);
            }

            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
            var accessToken = _jwtTokenGenerator.GenerateAccessToken(user, roles);
            var refreshTokenString = _jwtTokenGenerator.GenerateRefreshToken();

            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshTokenString,
                DeviceInfo = request.UserAgent,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            };
            await _userRepository.AddRefreshTokenAsync(refreshToken);
            await _userRepository.SaveChangesAsync();

            return new ApiResponse<AuthResponseDto>(
                new AuthResponseDto
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshTokenString,
                    ExpiresIn = 3600
                },
                "Login successful.",
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
