using System;
using System.Threading;
using System.Threading.Tasks;
using BannerAPI.DTOs;
using BannerAPI.Interfaces;
using MediatR;
using SharedKernel.Responses;

namespace BannerAPI.Application.Features.Banners.Queries
{
    public record GetBannerByIdQuery(Guid Id) : IRequest<ApiResponse<BannerDto>>;

    public class GetBannerByIdQueryHandler : IRequestHandler<GetBannerByIdQuery, ApiResponse<BannerDto>>
    {
        private readonly IBannerRepository _repository;

        public GetBannerByIdQueryHandler(IBannerRepository repository)
        {
            _repository = repository;
        }

        public async Task<ApiResponse<BannerDto>> Handle(GetBannerByIdQuery request, CancellationToken cancellationToken)
        {
            var banner = await _repository.GetByIdAsync(request.Id);
            if (banner is null)
            {
                return ApiResponse<BannerDto>.ErrorResponse("Banner not found.", 404);
            }

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

            return new ApiResponse<BannerDto>(dto, "Banner retrieved successfully.");
        }
    }
}
