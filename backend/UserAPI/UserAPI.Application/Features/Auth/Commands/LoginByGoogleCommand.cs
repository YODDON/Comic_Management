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
    public class LoginByGoogleCommand : IRequest<ApiResponse<AuthResponseDto>>
    {
        public GoogleLoginRequestDto Request { get; set; }
        public string UserAgent { get; set; }
        public string IpAddress { get; set; }
    }

    public class LoginByGoogleCommandHandler : IRequestHandler<LoginByGoogleCommand, ApiResponse<AuthResponseDto>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IEmailService _emailService;
        private readonly IAvatarStorageService _avatarStorageService;
        private readonly IMemoryCache _memoryCache;
        private readonly string _frontendUrl;

        public LoginByGoogleCommandHandler(IUserRepository userRepository, IJwtTokenGenerator jwtTokenGenerator, IEmailService emailService, IAvatarStorageService avatarStorageService, IMemoryCache memoryCache, IConfiguration configuration)
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

        public async Task<ApiResponse<AuthResponseDto>> Handle(LoginByGoogleCommand request, CancellationToken cancellationToken)
        {
            var clientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
            if (string.IsNullOrEmpty(clientId))
            {
                return ApiResponse<AuthResponseDto>.ErrorResponse("Google login is not configured on the server.", 500);
            }

            Google.Apis.Auth.GoogleJsonWebSignature.Payload payload;
            try
            {
                var settings = new Google.Apis.Auth.GoogleJsonWebSignature.ValidationSettings()
                {
                    Audience = new List<string>() { clientId }
                };
                payload = await Google.Apis.Auth.GoogleJsonWebSignature.ValidateAsync(request.Request.IdToken, settings);
            }
            catch (Exception)
            {
                return ApiResponse<AuthResponseDto>.ErrorResponse("Invalid Google token.", 401);
            }

            var user = await _userRepository.GetUserByEmailAsync(payload.Email);

            if (user == null)
            {
                user = new User
                {
                    Username = payload.Name ?? payload.Email.Split('@')[0],
                    Email = payload.Email,
                    PasswordHash = string.Empty,
                    IsEmailVerified = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _userRepository.AddUserAsync(user);
                
                // Google accounts receive the same default role as normal registrations.
                var role = await _userRepository.GetRoleByNameAsync("Reader");
                if (role != null)
                {
                    user.UserRoles.Add(new UserRole
                    {
                        RoleId = role.Id
                    });
                }

                await _userRepository.SaveChangesAsync();
            }
            else
            {
                if (!user.IsActive)
                {
                    return ApiResponse<AuthResponseDto>.ErrorResponse("User account is not active.", 403);
                }
                
                // If user registered normally but hasn't verified email, Google login auto-verifies it
                if (!user.IsEmailVerified)
                {
                    user.IsEmailVerified = true;
                    await _userRepository.SaveChangesAsync();
                }
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
                "Google login successful.",
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
