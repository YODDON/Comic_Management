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
    public record GetActiveBannersQuery : IRequest<ApiResponse<List<BannerDto>>>;

    public class GetActiveBannersQueryHandler : IRequestHandler<GetActiveBannersQuery, ApiResponse<List<BannerDto>>>
    {
        private readonly IBannerRepository _repository;

        public GetActiveBannersQueryHandler(IBannerRepository repository)
        {
            _repository = repository;
        }

        public async Task<ApiResponse<List<BannerDto>>> Handle(GetActiveBannersQuery request, CancellationToken cancellationToken)
        {
            var banners = await _repository.GetActiveAsync();
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

            return new ApiResponse<List<BannerDto>>(mapped, "Active banners retrieved successfully.");
        }
    }
}
