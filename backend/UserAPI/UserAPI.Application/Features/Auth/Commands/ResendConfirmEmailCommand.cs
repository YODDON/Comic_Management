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
    public class ResendConfirmEmailCommand : IRequest<ApiResponse<string>>
    {
        public ResendConfirmRequestDto Request { get; set; }
        public string IpAddress { get; set; }
    }

    public class ResendConfirmEmailCommandHandler : IRequestHandler<ResendConfirmEmailCommand, ApiResponse<string>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IEmailService _emailService;
        private readonly IAvatarStorageService _avatarStorageService;
        private readonly IMemoryCache _memoryCache;
        private readonly string _frontendUrl;

        public ResendConfirmEmailCommandHandler(IUserRepository userRepository, IJwtTokenGenerator jwtTokenGenerator, IEmailService emailService, IAvatarStorageService avatarStorageService, IMemoryCache memoryCache, IConfiguration configuration)
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

        public async Task<ApiResponse<string>> Handle(ResendConfirmEmailCommand request, CancellationToken cancellationToken)
        {
            var cacheKey = $"ResendEmail_{request.IpAddress}_{request.Request.Email}";
            if (_memoryCache.TryGetValue(cacheKey, out _))
            {
                return ApiResponse<string>.ErrorResponse("Please wait 1 minute before requesting another email.", 429);
            }

            var user = await _userRepository.GetUserByEmailAsync(request.Request.Email);
            
            // Return 200 even if user doesn't exist or is already verified to prevent enumeration
            if (user == null || user.IsEmailVerified)
            {
                return new ApiResponse<string>("If that email is in our database and unverified, we will send a verification link to it.", "Request processed.", 200);
            }

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
            await _userRepository.SaveChangesAsync();

            var verificationLink = $"{_frontendUrl}/verify-email?token={verificationCode.Code}";
            await _emailService.SendEmailAsync(
                user.Email,
                "Xác nhận tài khoản Comico của bạn",
                EmailTemplateBuilder.Verification(user.Username, verificationLink)
            );

            _memoryCache.Set(cacheKey, true, TimeSpan.FromMinutes(1));

            return new ApiResponse<string>("If that email is in our database and unverified, we will send a verification link to it.", "Request processed.", 200);
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
