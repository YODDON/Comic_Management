using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SocialAPI.Entities;

namespace SocialAPI.Interfaces
{
    public interface IFavoriteRepository
    {
        Task<Favorite?> GetFavoriteAsync(Guid userId, Guid comicId);
        Task AddFavoriteAsync(Favorite favorite);
        Task RemoveFavoriteAsync(Favorite favorite);
        Task<(List<Favorite> items, int total)> GetUserFavoritesAsync(Guid userId, int page, int pageSize);
    }
}
