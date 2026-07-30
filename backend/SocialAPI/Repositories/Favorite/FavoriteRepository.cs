using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SocialAPI.Data;
using SocialAPI.Entities;
using SocialAPI.Interfaces;

namespace SocialAPI.Repositories
{
    public class FavoriteRepository : IFavoriteRepository
    {
        private readonly SocialDbContext _context;

        public FavoriteRepository(SocialDbContext context)
        {
            _context = context;
        }

        public async Task<Favorite?> GetFavoriteAsync(Guid userId, Guid comicId)
        {
            return await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.ComicId == comicId);
        }

        public async Task AddFavoriteAsync(Favorite favorite)
        {
            _context.Favorites.Add(favorite);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveFavoriteAsync(Favorite favorite)
        {
            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();
        }

        public async Task<(List<Favorite> items, int total)> GetUserFavoritesAsync(Guid userId, int page, int pageSize)
        {
            var query = _context.Favorites.Where(f => f.UserId == userId);
            
            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(f => f.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }
    }
}
