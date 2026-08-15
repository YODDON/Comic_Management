using BannerAPI.DTOs;
using SharedKernel.Responses;

namespace BannerAPI.Interfaces;

public interface IBannerService
{
    Task<ApiResponse<List<BannerDto>>> GetActiveAsync();
    Task<ApiResponse<List<BannerDto>>> GetAllAsync();
    Task<ApiResponse<BannerDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<BannerDto>> CreateAsync(CreateBannerRequestDto request);
    Task<ApiResponse<BannerDto>> UpdateAsync(Guid id, UpdateBannerRequestDto request);
    Task<ApiResponse<bool>> DeleteAsync(Guid id);
    Task<ApiResponse<BannerImageUploadDto>> UploadImageAsync(IFormFile file);
}
