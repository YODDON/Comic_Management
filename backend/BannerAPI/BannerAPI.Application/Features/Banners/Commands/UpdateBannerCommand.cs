using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BannerAPI.DTOs;
using BannerAPI.Interfaces;
using MediatR;
using SharedKernel.Responses;

namespace BannerAPI.Application.Features.Banners.Commands
{
    public record UpdateBannerCommand(Guid Id, UpdateBannerRequestDto Request) : IRequest<ApiResponse<BannerDto>>;

    public class UpdateBannerCommandHandler : IRequestHandler<UpdateBannerCommand, ApiResponse<BannerDto>>
    {
        private readonly IBannerRepository _repository;

        public UpdateBannerCommandHandler(IBannerRepository repository)
        {
            _repository = repository;
        }

        public async Task<ApiResponse<BannerDto>> Handle(UpdateBannerCommand command, CancellationToken cancellationToken)
        {
            var id = command.Id;
            var request = command.Request;

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

            var dto = new BannerDto
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

            return new ApiResponse<BannerDto>(dto, "Banner updated successfully.");
        }

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
}
