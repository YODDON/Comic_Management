using System;
using System.Threading.Tasks;
using SharedKernel.Responses;
using SocialAPI.DTOs;

namespace SocialAPI.Interfaces
{
    public interface IFollowService
    {
        Task<ApiResponse<PagedResult<FollowDto>>> GetUserFollowsAsync(Guid followerId, int page, int pageSize);
        Task<ApiResponse<FollowDto>> AddFollowAsync(Guid followerId, Guid followingId);
        Task<ApiResponse<bool>> RemoveFollowAsync(Guid followerId, Guid followingId);
    }
}
