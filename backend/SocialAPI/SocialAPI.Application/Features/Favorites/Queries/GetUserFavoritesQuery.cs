using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SharedKernel.Responses;
using SharedKernel.Enums;
using SocialAPI.DTOs;
using SocialAPI.Entities;
using SocialAPI.Interfaces;

namespace SocialAPI.Application.Features.Favorites.Queries
{
    public class GetUserFavoritesQuery : IRequest<ApiResponse<PagedResult<FavoriteDto>>>
    {
        public Guid UserId { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class GetUserFavoritesQueryHandler : IRequestHandler<GetUserFavoritesQuery, ApiResponse<PagedResult<FavoriteDto>>>
    {
        private readonly IFavoriteRepository _repository;
        private readonly IComicValidator _comicValidator;

        public GetUserFavoritesQueryHandler(IFavoriteRepository repository, IComicValidator comicValidator)
        {
            _repository = repository;
            _comicValidator = comicValidator;
        }

        public async Task<ApiResponse<PagedResult<FavoriteDto>>> Handle(GetUserFavoritesQuery request, CancellationToken cancellationToken)
        {
            var (items, total) = await _repository.GetUserFavoritesAsync(request.UserId, request.Page, request.PageSize);
            
            var dtos = items.Select(f => new FavoriteDto
            {
                Id = f.Id,
                ComicId = f.ComicId,
                CreatedAt = f.CreatedAt
            }).ToList();

            var paginated = new PagedResult<FavoriteDto>(dtos, total, request.Page, request.PageSize);
            return new ApiResponse<PagedResult<FavoriteDto>>(paginated, "User favorites retrieved successfully.");
        }
    }
}
