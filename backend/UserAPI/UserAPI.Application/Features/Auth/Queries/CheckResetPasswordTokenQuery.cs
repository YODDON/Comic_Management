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

namespace UserAPI.Application.Features.Auth.Queries
{
    public class CheckResetPasswordTokenQuery : IRequest<ApiResponse<string>>
    {
        public string Token { get; set; }
    }

    public class CheckResetPasswordTokenQueryHandler : IRequestHandler<CheckResetPasswordTokenQuery, ApiResponse<string>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IEmailService _emailService;
        private readonly IAvatarStorageService _avatarStorageService;
        private readonly IMemoryCache _memoryCache;
        private readonly string _frontendUrl;

        public CheckResetPasswordTokenQueryHandler(IUserRepository userRepository, IJwtTokenGenerator jwtTokenGenerator, IEmailService emailService, IAvatarStorageService avatarStorageService, IMemoryCache memoryCache, IConfiguration configuration)
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

        public async Task<ApiResponse<string>> Handle(CheckResetPasswordTokenQuery request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetUserByVerificationCodeAsync(request.Token, "ResetPassword");
            if (user == null)
            {
                return ApiResponse<string>.ErrorResponse("Invalid or expired request.Token.", 400);
            }

            var verificationCode = user.VerificationCodes.First(vc => vc.Code == request.Token && vc.Type == "ResetPassword");

            if (user.IsEmailVerified && verificationCode.IsUsed)
            {
                return new ApiResponse<string>("Email already verified.", "Email đã được xác nhận trước đó.", 200);
            }

            if (verificationCode.IsUsed || verificationCode.ExpiresAt < DateTime.UtcNow)
            {
                return ApiResponse<string>.ErrorResponse("Invalid or expired request.Token.", 400);
            }

            return new ApiResponse<string>("Token is valid.", "Valid request.Token.", 200);
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
