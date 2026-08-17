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

namespace SocialAPI.Application.Features.Follows.Queries
{
    public class GetUserFollowsQuery : IRequest<ApiResponse<PagedResult<FollowDto>>>
    {
        public Guid UserId { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class GetUserFollowsQueryHandler : IRequestHandler<GetUserFollowsQuery, ApiResponse<PagedResult<FollowDto>>>
    {
        private readonly IFollowRepository _repository;

        public GetUserFollowsQueryHandler(IFollowRepository repository)
        {
            _repository = repository;
        }

        public async Task<ApiResponse<PagedResult<FollowDto>>> Handle(GetUserFollowsQuery request, CancellationToken cancellationToken)
        {
            var (items, total) = await _repository.GetUserFollowsAsync(request.UserId, request.Page, request.PageSize);
            
            var dtos = items.Select(f => new FollowDto
            {
                Id = f.Id,
                FollowerId = f.FollowerId,
                FollowingId = f.FollowingId,
                CreatedAt = f.CreatedAt
            }).ToList();

            var paginated = new PagedResult<FollowDto>(dtos, total, request.Page, request.PageSize);
            return new ApiResponse<PagedResult<FollowDto>>(paginated, "User follows retrieved successfully.");
        }
    }
}
