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
    public class RegisterCommand : IRequest<ApiResponse<AuthResponseDto>>
    {
        public RegisterRequestDto Request { get; set; }
        public string UserAgent { get; set; }
    }

    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, ApiResponse<AuthResponseDto>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IEmailService _emailService;
        private readonly IAvatarStorageService _avatarStorageService;
        private readonly IMemoryCache _memoryCache;
        private readonly string _frontendUrl;

        public RegisterCommandHandler(IUserRepository userRepository, IJwtTokenGenerator jwtTokenGenerator, IEmailService emailService, IAvatarStorageService avatarStorageService, IMemoryCache memoryCache, IConfiguration configuration)
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

        public async Task<ApiResponse<AuthResponseDto>> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            var normalizedEmail = request.Request.Email.Trim().ToLowerInvariant();

            var existingUser = await _userRepository.GetUserByEmailAsync(normalizedEmail);
            if (existingUser != null)
            {
                return ApiResponse<AuthResponseDto>.ErrorResponse("Email is already registered.", 409);
            }

            if (!IsPasswordStrong(request.Request.Password))
            {
                return ApiResponse<AuthResponseDto>.ErrorResponse("Password must be at least 8 characters long, contain at least one uppercase letter, one number, and one special character.", 422);
            }

            var user = new User
            {
                Username = request.Request.Username,
                Email = normalizedEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Request.Password),
                IsEmailVerified = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            // Unverified accounts start as Guest and become Reader after email verification.
            var guestRole = await _userRepository.GetRoleByNameAsync("Guest");
            if (guestRole != null)
            {
                user.UserRoles.Add(new UserRole
                {
                    RoleId = guestRole.Id,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Save user first to get the auto-incremented Id
            await _userRepository.AddUserAsync(user);
            await _userRepository.SaveChangesAsync();

            var verificationCode = new VerificationCode
            {
                UserId = user.Id,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                Type = "EmailVerification",
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };
            await _userRepository.AddVerificationCodeAsync(verificationCode);

            // Do not issue authentication tokens until the email is verified.
            await _userRepository.SaveChangesAsync();

            var verificationLink = $"{_frontendUrl}/verify-email?token={verificationCode.Code}";
            await _emailService.SendEmailAsync(
                user.Email,
                "Xác nhận tài khoản Comico của bạn",
                EmailTemplateBuilder.Verification(user.Username, verificationLink)
            );

            return new ApiResponse<AuthResponseDto>(
                null,
                "Đăng ký thành công. Vui lòng kiểm tra email để xác nhận tài khoản.",
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
