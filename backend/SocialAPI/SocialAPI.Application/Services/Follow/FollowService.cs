using System;
using System.Linq;
using System.Threading.Tasks;
using SharedKernel.Responses;
using SocialAPI.DTOs;
using SocialAPI.Entities;
using SocialAPI.Interfaces;

namespace SocialAPI.Services
{
    public class FollowService : IFollowService
    {
        private readonly IFollowRepository _repository;

        public FollowService(IFollowRepository repository)
        {
            _repository = repository;
        }

        public async Task<ApiResponse<PagedResult<FollowDto>>> GetUserFollowsAsync(Guid followerId, int page, int pageSize)
        {
            var (items, total) = await _repository.GetUserFollowsAsync(followerId, page, pageSize);
            
            var dtos = items.Select(f => new FollowDto
            {
                Id = f.Id,
                FollowerId = f.FollowerId,
                FollowingId = f.FollowingId,
                CreatedAt = f.CreatedAt
            }).ToList();

            var paginated = new PagedResult<FollowDto>(dtos, total, page, pageSize);
            return new ApiResponse<PagedResult<FollowDto>>(paginated, "User follows retrieved successfully.");
        }

        public async Task<ApiResponse<FollowDto>> AddFollowAsync(Guid followerId, Guid followingId)
        {
            if (followerId == followingId)
            {
                return new ApiResponse<FollowDto>(null, "You cannot follow yourself.", 400);
            }

            var existingFollow = await _repository.GetFollowAsync(followerId, followingId);
            if (existingFollow != null)
            {
                return new ApiResponse<FollowDto>(null, "You are already following this user.", 409);
            }

            var follow = new Follow
            {
                FollowerId = followerId,
                FollowingId = followingId,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddFollowAsync(follow);

            var dto = new FollowDto
            {
                Id = follow.Id,
                FollowerId = follow.FollowerId,
                FollowingId = follow.FollowingId,
                CreatedAt = follow.CreatedAt
            };

            return new ApiResponse<FollowDto>(dto, "Started following user successfully.");
        }

        public async Task<ApiResponse<bool>> RemoveFollowAsync(Guid followerId, Guid followingId)
        {
            var existingFollow = await _repository.GetFollowAsync(followerId, followingId);
            if (existingFollow == null)
            {
                return new ApiResponse<bool>(false, "You are not following this user.", 404);
            }

            await _repository.RemoveFollowAsync(existingFollow);

            return new ApiResponse<bool>(true, "Stopped following user successfully.");
        }
    }
}
