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
    public class ChangePasswordCommand : IRequest<ApiResponse<string>>
    {
        public int UserId { get; set; }
        public ChangePasswordRequestDto Request { get; set; }
    }

    public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, ApiResponse<string>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IEmailService _emailService;
        private readonly IAvatarStorageService _avatarStorageService;
        private readonly IMemoryCache _memoryCache;
        private readonly string _frontendUrl;

        public ChangePasswordCommandHandler(IUserRepository userRepository, IJwtTokenGenerator jwtTokenGenerator, IEmailService emailService, IAvatarStorageService avatarStorageService, IMemoryCache memoryCache, IConfiguration configuration)
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

        public async Task<ApiResponse<string>> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetUserByIdAsync(request.UserId);
            if (user == null)
            {
                return ApiResponse<string>.ErrorResponse("User not found.", 404);
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Request.CurrentPassword, user.PasswordHash))
            {
                return ApiResponse<string>.ErrorResponse("Current password is incorrect.", 400);
            }

            if (!IsPasswordStrong(request.Request.NewPassword))
            {
                return ApiResponse<string>.ErrorResponse("New password must be at least 8 characters long and contain an uppercase letter, a number, and a special character.", 422);
            }

            if (BCrypt.Net.BCrypt.Verify(request.Request.NewPassword, user.PasswordHash))
            {
                return ApiResponse<string>.ErrorResponse("New password must be different from the current password.", 422);
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.SaveChangesAsync();

            return new ApiResponse<string>(null, "Password changed successfully.", 200);
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
