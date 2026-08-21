using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SocialAPI.Entities;

namespace SocialAPI.Interfaces
{
    public interface IFollowRepository
    {
        Task<Follow?> GetFollowAsync(Guid followerId, Guid followingId);
        Task AddFollowAsync(Follow follow);
        Task RemoveFollowAsync(Follow follow);
        Task<(List<Follow> items, int total)> GetUserFollowsAsync(Guid followerId, int page, int pageSize);
    }
}
