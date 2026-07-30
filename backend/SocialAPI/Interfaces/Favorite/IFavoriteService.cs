using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SharedKernel.Responses;
using SocialAPI.DTOs;

namespace SocialAPI.Interfaces
{
    public interface IFavoriteService
    {
        Task<ApiResponse<PagedResult<FavoriteDto>>> GetUserFavoritesAsync(Guid userId, int page, int pageSize);
        Task<ApiResponse<FavoriteDto>> AddFavoriteAsync(Guid userId, Guid comicId);
        Task<ApiResponse<bool>> RemoveFavoriteAsync(Guid userId, Guid comicId);
    }
}
