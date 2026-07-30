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
    public class FollowRepository : IFollowRepository
    {
        private readonly SocialDbContext _context;

        public FollowRepository(SocialDbContext context)
        {
            _context = context;
        }

        public async Task<Follow?> GetFollowAsync(Guid followerId, Guid followingId)
        {
            return await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowingId == followingId);
        }

        public async Task AddFollowAsync(Follow follow)
        {
            _context.Follows.Add(follow);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveFollowAsync(Follow follow)
        {
            _context.Follows.Remove(follow);
            await _context.SaveChangesAsync();
        }

        public async Task<(List<Follow> items, int total)> GetUserFollowsAsync(Guid followerId, int page, int pageSize)
        {
            var query = _context.Follows.Where(f => f.FollowerId == followerId);
            
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
