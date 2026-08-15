using BannerAPI.DTOs;
using BannerAPI.Entities;
using BannerAPI.Interfaces;
using SharedKernel.Responses;

namespace BannerAPI.Services;

public class BannerService : IBannerService
{
    private readonly IBannerRepository _repository;
    private readonly ICloudinaryService _cloudinaryService;

    public BannerService(IBannerRepository repository, ICloudinaryService cloudinaryService)
    {
        _repository = repository;
        _cloudinaryService = cloudinaryService;
    }

    public async Task<ApiResponse<List<BannerDto>>> GetActiveAsync()
    {
        var banners = await _repository.GetActiveAsync();
        return new ApiResponse<List<BannerDto>>(banners.Select(Map).ToList(), "Active banners retrieved successfully.");
    }

    public async Task<ApiResponse<List<BannerDto>>> GetAllAsync()
    {
        var banners = await _repository.GetAllAsync();
        return new ApiResponse<List<BannerDto>>(banners.Select(Map).ToList(), "Banners retrieved successfully.");
    }

    public async Task<ApiResponse<BannerDto>> GetByIdAsync(Guid id)
    {
        var banner = await _repository.GetByIdAsync(id);
        return banner is null
            ? ApiResponse<BannerDto>.ErrorResponse("Banner not found.", 404)
            : new ApiResponse<BannerDto>(Map(banner), "Banner retrieved successfully.");
    }

    public async Task<ApiResponse<BannerDto>> CreateAsync(CreateBannerRequestDto request)
    {
        var title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim();
        if (title is not null && await _repository.TitleExistsAsync(title))
        {
            return ApiResponse<BannerDto>.ErrorResponse("Tiêu đề banner đã tồn tại.", 409);
        }
        if (await _repository.DisplayOrderExistsAsync(request.DisplayOrder))
        {
            return ApiResponse<BannerDto>.ErrorResponse($"Thứ tự hiển thị {request.DisplayOrder} đã được sử dụng.", 409);
        }

        var banner = new Banner
        {
            Title = title,
            ImageUrl = request.ImageUrl.Trim(),
            ImagePublicId = string.IsNullOrWhiteSpace(request.ImagePublicId)
                ? ExtractCloudinaryPublicId(request.ImageUrl)
                : request.ImagePublicId.Trim(),
            TargetUrl = string.IsNullOrWhiteSpace(request.LinkUrl) ? null : request.LinkUrl.Trim(),
            IsActive = request.IsActive,
            DisplayOrder = request.DisplayOrder
        };

        await _repository.AddAsync(banner);
        return new ApiResponse<BannerDto>(Map(banner), "Banner created successfully.", 201);
    }

    public async Task<ApiResponse<BannerDto>> UpdateAsync(Guid id, UpdateBannerRequestDto request)
    {
        var banner = await _repository.GetByIdAsync(id);
        if (banner is null)
        {
            return ApiResponse<BannerDto>.ErrorResponse("Banner not found.", 404);
        }

        var title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim();
        if (title is not null && await _repository.TitleExistsAsync(title, id))
        {
            return ApiResponse<BannerDto>.ErrorResponse("Tiêu đề banner đã tồn tại.", 409);
        }
        if (await _repository.DisplayOrderExistsAsync(request.DisplayOrder, id))
        {
            return ApiResponse<BannerDto>.ErrorResponse($"Thứ tự hiển thị {request.DisplayOrder} đã được sử dụng.", 409);
        }

        banner.Title = title;
        banner.ImageUrl = request.ImageUrl.Trim();
        banner.ImagePublicId = string.IsNullOrWhiteSpace(request.ImagePublicId)
            ? ExtractCloudinaryPublicId(request.ImageUrl)
            : request.ImagePublicId.Trim();
        banner.TargetUrl = string.IsNullOrWhiteSpace(request.LinkUrl) ? null : request.LinkUrl.Trim();
        banner.IsActive = request.IsActive;
        banner.DisplayOrder = request.DisplayOrder;
        banner.UpdatedAt = DateTime.UtcNow;
        await _repository.SaveChangesAsync();

        return new ApiResponse<BannerDto>(Map(banner), "Banner updated successfully.");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
    {
        var banner = await _repository.GetByIdAsync(id);
        if (banner is null)
        {
            return ApiResponse<bool>.ErrorResponse("Banner not found.", 404);
        }

        if (!string.IsNullOrWhiteSpace(banner.ImagePublicId)
            && !await _cloudinaryService.DeleteImageAsync(banner.ImagePublicId))
        {
            return ApiResponse<bool>.ErrorResponse("Could not delete the banner image from Cloudinary.", 502);
        }

        _repository.Remove(banner);
        await _repository.SaveChangesAsync();
        return new ApiResponse<bool>(true, "Banner deleted successfully.");
    }

    public async Task<ApiResponse<BannerImageUploadDto>> UploadImageAsync(IFormFile file)
    {
        try
        {
            var result = await _cloudinaryService.UploadImageAsync(file);
            return new ApiResponse<BannerImageUploadDto>(new BannerImageUploadDto
            {
                ImageUrl = result.Url,
                ImagePublicId = result.PublicId
            }, "Banner image uploaded successfully.");
        }
        catch (ArgumentException error)
        {
            return ApiResponse<BannerImageUploadDto>.ErrorResponse(error.Message, 400);
        }
    }

    private static BannerDto Map(Banner banner) => new()
    {
        Id = banner.Id,
        Title = banner.Title,
        ImageUrl = banner.ImageUrl,
        LinkUrl = banner.TargetUrl,
        IsActive = banner.IsActive,
        DisplayOrder = banner.DisplayOrder,
        CreatedAt = banner.CreatedAt,
        UpdatedAt = banner.UpdatedAt
    };

    private static string? ExtractCloudinaryPublicId(string imageUrl)
    {
        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri)) return null;
        var marker = "/upload/";
        var index = uri.AbsolutePath.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0) return null;

        var path = Uri.UnescapeDataString(uri.AbsolutePath[(index + marker.Length)..]);
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();
        if (segments.Count > 0 && segments[0].StartsWith('v') && segments[0][1..].All(char.IsDigit))
        {
            segments.RemoveAt(0);
        }

        if (segments.Count == 0) return null;
        segments[^1] = Path.GetFileNameWithoutExtension(segments[^1]);
        return string.Join('/', segments);
    }
}
