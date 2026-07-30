using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SharedKernel.Responses;
using SocialAPI.DTOs;
using SocialAPI.Entities;
using SocialAPI.Interfaces;

namespace SocialAPI.Services
{
    public class FavoriteService : IFavoriteService
    {
        private readonly IFavoriteRepository _repository;
        private readonly IComicValidator _comicValidator;

        public FavoriteService(IFavoriteRepository repository, IComicValidator comicValidator)
        {
            _repository = repository;
            _comicValidator = comicValidator;
        }

        public async Task<ApiResponse<PagedResult<FavoriteDto>>> GetUserFavoritesAsync(Guid userId, int page, int pageSize)
        {
            var (items, total) = await _repository.GetUserFavoritesAsync(userId, page, pageSize);
            
            var dtos = items.Select(f => new FavoriteDto
            {
                Id = f.Id,
                ComicId = f.ComicId,
                CreatedAt = f.CreatedAt
            }).ToList();

            var paginated = new PagedResult<FavoriteDto>(dtos, total, page, pageSize);
            return new ApiResponse<PagedResult<FavoriteDto>>(paginated, "User favorites retrieved successfully.");
        }

        public async Task<ApiResponse<FavoriteDto>> AddFavoriteAsync(Guid userId, Guid comicId)
        {
            var exists = await _comicValidator.ExistsAsync(comicId);
            if (!exists)
            {
                return new ApiResponse<FavoriteDto>(null, "Comic not found.", 404);
            }

            var existingFav = await _repository.GetFavoriteAsync(userId, comicId);
            if (existingFav != null)
            {
                return new ApiResponse<FavoriteDto>(null, "Comic is already in favorites.", 409);
            }

            var fav = new Favorite
            {
                UserId = userId,
                ComicId = comicId,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddFavoriteAsync(fav);

            var dto = new FavoriteDto
            {
                Id = fav.Id,
                ComicId = fav.ComicId,
                CreatedAt = fav.CreatedAt
            };

            return new ApiResponse<FavoriteDto>(dto, "Added to favorites successfully.");
        }

        public async Task<ApiResponse<bool>> RemoveFavoriteAsync(Guid userId, Guid comicId)
        {
            var existingFav = await _repository.GetFavoriteAsync(userId, comicId);
            if (existingFav == null)
            {
                return new ApiResponse<bool>(false, "Comic is not in favorites.", 404);
            }

            await _repository.RemoveFavoriteAsync(existingFav);

            return new ApiResponse<bool>(true, "Removed from favorites successfully.");
        }
    }
}
