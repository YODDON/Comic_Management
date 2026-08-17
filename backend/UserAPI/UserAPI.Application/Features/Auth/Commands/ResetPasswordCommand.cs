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
    public class ResetPasswordCommand : IRequest<ApiResponse<string>>
    {
        public ResetPasswordRequestDto Request { get; set; }
    }

    public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, ApiResponse<string>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IEmailService _emailService;
        private readonly IAvatarStorageService _avatarStorageService;
        private readonly IMemoryCache _memoryCache;
        private readonly string _frontendUrl;

        public ResetPasswordCommandHandler(IUserRepository userRepository, IJwtTokenGenerator jwtTokenGenerator, IEmailService emailService, IAvatarStorageService avatarStorageService, IMemoryCache memoryCache, IConfiguration configuration)
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

        public async Task<ApiResponse<string>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetUserByVerificationCodeAsync(request.Request.Token, "ResetPassword");
            if (user == null)
            {
                return ApiResponse<string>.ErrorResponse("Invalid or expired token.", 400);
            }

            var verificationCode = user.VerificationCodes.First(vc => vc.Code == request.Request.Token && vc.Type == "ResetPassword");

            if (verificationCode.IsUsed || verificationCode.ExpiresAt < DateTime.UtcNow)
            {
                return ApiResponse<string>.ErrorResponse("Invalid or expired token.", 400);
            }

            if (!IsPasswordStrong(request.Request.NewPassword))
            {
                return ApiResponse<string>.ErrorResponse("Password must be at least 8 characters long and contain at least one uppercase letter, one lowercase letter, one number, and one special character.", 422);
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Request.NewPassword);
            verificationCode.IsUsed = true;

            // Optional: revoke all refresh tokens so the user has to login again
            foreach (var rt in user.RefreshTokens)
            {
                rt.IsRevoked = true;
            }

            await _userRepository.SaveChangesAsync();

            return new ApiResponse<string>("Password has been successfully reset.", "Reset successful.", 200);
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
