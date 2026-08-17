using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BannerAPI.DTOs;
using BannerAPI.Entities;
using BannerAPI.Interfaces;
using MediatR;
using SharedKernel.Responses;

namespace BannerAPI.Application.Features.Banners.Commands
{
    public record CreateBannerCommand(CreateBannerRequestDto Request) : IRequest<ApiResponse<BannerDto>>;

    public class CreateBannerCommandHandler : IRequestHandler<CreateBannerCommand, ApiResponse<BannerDto>>
    {
        private readonly IBannerRepository _repository;

        public CreateBannerCommandHandler(IBannerRepository repository)
        {
            _repository = repository;
        }

        public async Task<ApiResponse<BannerDto>> Handle(CreateBannerCommand command, CancellationToken cancellationToken)
        {
            var request = command.Request;
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

            return new ApiResponse<BannerDto>(dto, "Banner created successfully.", 201);
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
