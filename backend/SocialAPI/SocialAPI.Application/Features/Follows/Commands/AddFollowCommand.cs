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

namespace SocialAPI.Application.Features.Follows.Commands
{
    public class AddFollowCommand : IRequest<ApiResponse<FollowDto>>
    {
        public Guid UserId { get; set; }
        public Guid FollowingId { get; set; }
    }

    public class AddFollowCommandHandler : IRequestHandler<AddFollowCommand, ApiResponse<FollowDto>>
    {
        private readonly IFollowRepository _repository;

        public AddFollowCommandHandler(IFollowRepository repository)
        {
            _repository = repository;
        }

        public async Task<ApiResponse<FollowDto>> Handle(AddFollowCommand request, CancellationToken cancellationToken)
        {
            if (request.UserId == request.FollowingId)
            {
                return new ApiResponse<FollowDto>(null, "You cannot follow yourself.", 400);
            }

            var existingFollow = await _repository.GetFollowAsync(request.UserId, request.FollowingId);
            if (existingFollow != null)
            {
                return new ApiResponse<FollowDto>(null, "You are already following this user.", 409);
            }

            var follow = new Follow
            {
                FollowerId = request.UserId,
                FollowingId = request.FollowingId,
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
    }
}
