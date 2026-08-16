using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using SharedKernel.Responses;

using UserAPI.Application.DTOs;
using UserAPI.Domain.Entities;
using UserAPI.Application.Interfaces; using UserAPI.Domain.Interfaces;     

namespace UserAPI.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IEmailService _emailService;
        private readonly IAvatarStorageService _avatarStorageService;
        private readonly IMemoryCache _memoryCache;
        private readonly string _frontendUrl;

        public AuthService(IUserRepository userRepository, IJwtTokenGenerator jwtTokenGenerator, IEmailService emailService, IAvatarStorageService avatarStorageService, IMemoryCache memoryCache, IConfiguration configuration)
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

        public async Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterRequestDto request, string userAgent)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            var existingUser = await _userRepository.GetUserByEmailAsync(normalizedEmail);
            if (existingUser != null)
            {
                return ApiResponse<AuthResponseDto>.ErrorResponse("Email is already registered.", 409);
            }

            if (!IsPasswordStrong(request.Password))
            {
                return ApiResponse<AuthResponseDto>.ErrorResponse("Password must be at least 8 characters long, contain at least one uppercase letter, one number, and one special character.", 422);
            }

            var user = new User
            {
                Username = request.Username,
                Email = normalizedEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
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

        public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto request, string userAgent, string ipAddress)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var cacheKey = $"LoginAttempts_{ipAddress}_{normalizedEmail}";
            _memoryCache.TryGetValue(cacheKey, out int failedAttempts);

            if (failedAttempts >= 5)
            {
                return ApiResponse<AuthResponseDto>.ErrorResponse("Too many login attempts. Please try again in 15 minutes.", 429);
            }

            var user = await _userRepository.GetUserByEmailAsync(normalizedEmail);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
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
                DeviceInfo = userAgent,
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

        public async Task<ApiResponse<UserProfileDto>> GetProfileAsync(int userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<UserProfileDto>.ErrorResponse("User not found.", 404);
            }

            var profileDto = new UserProfileDto
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

            return new ApiResponse<UserProfileDto>(profileDto, "Profile retrieved successfully.", 200);
        }

        public async Task<ApiResponse<UserProfileDto>> UpdateProfileAsync(int userId, UpdateProfileRequestDto request)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<UserProfileDto>.ErrorResponse("User not found.", 404);
            }

            user.Username = request.Username.Trim();
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.SaveChangesAsync();

            return new ApiResponse<UserProfileDto>(ToProfileDto(user), "Profile updated successfully.", 200);
        }

        public async Task<ApiResponse<string>> ChangePasswordAsync(int userId, ChangePasswordRequestDto request)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<string>.ErrorResponse("User not found.", 404);
            }

            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            {
                return ApiResponse<string>.ErrorResponse("Current password is incorrect.", 400);
            }

            if (!IsPasswordStrong(request.NewPassword))
            {
                return ApiResponse<string>.ErrorResponse("New password must be at least 8 characters long and contain an uppercase letter, a number, and a special character.", 422);
            }

            if (BCrypt.Net.BCrypt.Verify(request.NewPassword, user.PasswordHash))
            {
                return ApiResponse<string>.ErrorResponse("New password must be different from the current password.", 422);
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.SaveChangesAsync();

            return new ApiResponse<string>(null, "Password changed successfully.", 200);
        }

        public async Task<ApiResponse<UserProfileDto>> UpdateAvatarAsync(int userId, IFormFile avatar)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<UserProfileDto>.ErrorResponse("User not found.", 404);
            }

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
            if (avatar.Length == 0 || avatar.Length > 5 * 1024 * 1024 || !allowedTypes.Contains(avatar.ContentType.ToLowerInvariant()))
            {
                return ApiResponse<UserProfileDto>.ErrorResponse("Avatar must be a JPG, PNG, or WEBP image no larger than 5 MB.", 400);
            }

            user.AvatarUrl = await _avatarStorageService.UploadAsync(avatar, userId);
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.SaveChangesAsync();

            return new ApiResponse<UserProfileDto>(ToProfileDto(user), "Avatar updated successfully.", 200);
        }

        private static UserProfileDto ToProfileDto(User user) => new()
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

        public async Task<ApiResponse<string>> LogoutAsync(int userId, LogoutRequestDto request)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<string>.ErrorResponse("User not found.", 404);
            }

            var refreshToken = user.RefreshTokens.FirstOrDefault(rt => rt.Token == request.RefreshToken);
            if (refreshToken == null || refreshToken.IsRevoked)
            {
                // To be safe, if they send an invalid token, we still return success to not leak information,
                // or we return 400. Let's return 400.
                return ApiResponse<string>.ErrorResponse("Invalid refresh token.", 400);
            }

            refreshToken.IsRevoked = true;
            await _userRepository.SaveChangesAsync();

            return new ApiResponse<string>("Successfully logged out.", "Logout successful.", 200);
        }

        public async Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request, string userAgent)
        {
            var user = await _userRepository.GetUserByRefreshTokenAsync(request.RefreshToken);
            if (user == null)
            {
                return ApiResponse<AuthResponseDto>.ErrorResponse("Invalid refresh token.", 401);
            }

            if (!user.IsEmailVerified)
            {
                return ApiResponse<AuthResponseDto>.ErrorResponse("Email chưa được xác nhận.", 403);
            }

            var existingToken = user.RefreshTokens.First(rt => rt.Token == request.RefreshToken);

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

            if (existingToken.DeviceInfo != userAgent)
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
                DeviceInfo = userAgent,
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

        public async Task<ApiResponse<string>> ForgotPasswordAsync(ForgotPasswordRequestDto request)
        {
            var user = await _userRepository.GetUserByEmailAsync(request.Email);
            if (user == null)
            {
                // Always return 200 OK to prevent email enumeration
                return new ApiResponse<string>("If that email is in our database, we will send a password reset link to it.", "Request processed.", 200);
            }

            var resetCode = new VerificationCode
            {
                UserId = user.Id,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                Type = "ResetPassword",
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddVerificationCodeAsync(resetCode);
            await _userRepository.SaveChangesAsync();

            var resetLink = $"{_frontendUrl}/reset-password?token={resetCode.Code}";
            await _emailService.SendEmailAsync(
                user.Email,
                "Đặt lại mật khẩu Comico",
                EmailTemplateBuilder.PasswordReset(user.Username, resetLink)
            );

            return new ApiResponse<string>("If that email is in our database, we will send a password reset link to it.", "Request processed.", 200);
        }

        public async Task<ApiResponse<string>> CheckResetPasswordTokenAsync(string token)
        {
            var user = await _userRepository.GetUserByVerificationCodeAsync(token, "ResetPassword");
            if (user == null)
            {
                return ApiResponse<string>.ErrorResponse("Invalid or expired token.", 400);
            }

            var verificationCode = user.VerificationCodes.First(vc => vc.Code == token && vc.Type == "ResetPassword");

            if (user.IsEmailVerified && verificationCode.IsUsed)
            {
                return new ApiResponse<string>("Email already verified.", "Email đã được xác nhận trước đó.", 200);
            }

            if (verificationCode.IsUsed || verificationCode.ExpiresAt < DateTime.UtcNow)
            {
                return ApiResponse<string>.ErrorResponse("Invalid or expired token.", 400);
            }

            return new ApiResponse<string>("Token is valid.", "Valid token.", 200);
        }

        public async Task<ApiResponse<string>> ResetPasswordAsync(ResetPasswordRequestDto request)
        {
            var user = await _userRepository.GetUserByVerificationCodeAsync(request.Token, "ResetPassword");
            if (user == null)
            {
                return ApiResponse<string>.ErrorResponse("Invalid or expired token.", 400);
            }

            var verificationCode = user.VerificationCodes.First(vc => vc.Code == request.Token && vc.Type == "ResetPassword");

            if (verificationCode.IsUsed || verificationCode.ExpiresAt < DateTime.UtcNow)
            {
                return ApiResponse<string>.ErrorResponse("Invalid or expired token.", 400);
            }

            if (!IsPasswordStrong(request.NewPassword))
            {
                return ApiResponse<string>.ErrorResponse("Password must be at least 8 characters long and contain at least one uppercase letter, one lowercase letter, one number, and one special character.", 422);
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            verificationCode.IsUsed = true;

            // Optional: revoke all refresh tokens so the user has to login again
            foreach (var rt in user.RefreshTokens)
            {
                rt.IsRevoked = true;
            }

            await _userRepository.SaveChangesAsync();

            return new ApiResponse<string>("Password has been successfully reset.", "Reset successful.", 200);
        }

        public async Task<ApiResponse<string>> VerifyEmailAsync(string token)
        {
            var user = await _userRepository.GetUserByVerificationCodeAsync(token, "EmailVerification");
            if (user == null)
            {
                return ApiResponse<string>.ErrorResponse("Invalid or expired token.", 400);
            }

            var verificationCode = user.VerificationCodes.First(vc => vc.Code == token && vc.Type == "EmailVerification");

            if (verificationCode.IsUsed || verificationCode.ExpiresAt < DateTime.UtcNow)
            {
                return ApiResponse<string>.ErrorResponse("Invalid or expired token.", 400);
            }

            user.IsEmailVerified = true;
            verificationCode.IsUsed = true;
            var guestAssignments = user.UserRoles
                .Where(userRole => userRole.Role.Name == "Guest")
                .ToList();
            foreach (var assignment in guestAssignments)
            {
                user.UserRoles.Remove(assignment);
            }
            var readerRole = await _userRepository.GetRoleByNameAsync("Reader");
            if (readerRole != null && user.UserRoles.All(userRole => userRole.RoleId != readerRole.Id))
            {
                user.UserRoles.Add(new UserRole
                {
                    RoleId = readerRole.Id,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await _userRepository.SaveChangesAsync();

            return new ApiResponse<string>("Email verified successfully.", "Verification successful.", 200);
        }

        public async Task<ApiResponse<string>> ResendConfirmEmailAsync(ResendConfirmRequestDto request, string ipAddress)
        {
            var cacheKey = $"ResendEmail_{ipAddress}_{request.Email}";
            if (_memoryCache.TryGetValue(cacheKey, out _))
            {
                return ApiResponse<string>.ErrorResponse("Please wait 1 minute before requesting another email.", 429);
            }

            var user = await _userRepository.GetUserByEmailAsync(request.Email);
            
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

        public async Task<ApiResponse<AuthResponseDto>> LoginByGoogleAsync(GoogleLoginRequestDto request, string userAgent, string ipAddress)
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
                payload = await Google.Apis.Auth.GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
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
                DeviceInfo = userAgent,
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
            if (string.IsNullOrEmpty(password) || password.Length < 8) return false;
            if (!Regex.IsMatch(password, @"[A-Z]")) return false; // uppercase
            if (!Regex.IsMatch(password, @"[0-9]")) return false; // number
            if (!Regex.IsMatch(password, @"[\W_]")) return false; // special character
            return true;
        }
    }
}
