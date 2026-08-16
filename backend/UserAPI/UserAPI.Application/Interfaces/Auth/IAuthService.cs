using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using SharedKernel.Responses;
using UserAPI.Application.DTOs;

namespace UserAPI.Application.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterRequestDto request, string userAgent);
        Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto request, string userAgent, string ipAddress);
        Task<ApiResponse<UserProfileDto>> GetProfileAsync(int userId);
        Task<ApiResponse<UserProfileDto>> UpdateProfileAsync(int userId, UpdateProfileRequestDto request);
        Task<ApiResponse<string>> ChangePasswordAsync(int userId, ChangePasswordRequestDto request);
        Task<ApiResponse<UserProfileDto>> UpdateAvatarAsync(int userId, IFormFile avatar);
        Task<ApiResponse<string>> LogoutAsync(int userId, LogoutRequestDto request);
        Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request, string userAgent);
        Task<ApiResponse<string>> ForgotPasswordAsync(ForgotPasswordRequestDto request);
        Task<ApiResponse<string>> CheckResetPasswordTokenAsync(string token);
        Task<ApiResponse<string>> ResetPasswordAsync(ResetPasswordRequestDto request);
        Task<ApiResponse<string>> VerifyEmailAsync(string token);
        Task<ApiResponse<string>> ResendConfirmEmailAsync(ResendConfirmRequestDto request, string ipAddress);
        Task<ApiResponse<AuthResponseDto>> LoginByGoogleAsync(GoogleLoginRequestDto request, string userAgent, string ipAddress);
    }
}
