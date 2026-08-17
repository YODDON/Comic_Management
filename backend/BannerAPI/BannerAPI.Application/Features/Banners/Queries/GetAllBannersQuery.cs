using BannerAPI.DTOs;
using BannerAPI.Interfaces;
using MediatR;
using SharedKernel.Responses;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BannerAPI.Application.Features.Banners.Queries
{
    public record GetAllBannersQuery : IRequest<ApiResponse<List<BannerDto>>>;

    public class GetAllBannersQueryHandler : IRequestHandler<GetAllBannersQuery, ApiResponse<List<BannerDto>>>
    {
        private readonly IBannerRepository _repository;

        public GetAllBannersQueryHandler(IBannerRepository repository)
        {
            _repository = repository;
        }

        public async Task<ApiResponse<List<BannerDto>>> Handle(GetAllBannersQuery request, CancellationToken cancellationToken)
        {
            var banners = await _repository.GetAllAsync();
            var mapped = banners.Select(banner => new BannerDto
            {
                Id = banner.Id,
                Title = banner.Title,
                ImageUrl = banner.ImageUrl,
                LinkUrl = banner.TargetUrl,
                IsActive = banner.IsActive,
                DisplayOrder = banner.DisplayOrder,
                CreatedAt = banner.CreatedAt,
                UpdatedAt = banner.UpdatedAt
            }).ToList();

            return new ApiResponse<List<BannerDto>>(mapped, "Banners retrieved successfully.");
        }
    }
}
