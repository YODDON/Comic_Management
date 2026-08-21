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
    public class RemoveFollowCommand : IRequest<ApiResponse<bool>>
    {
        public Guid UserId { get; set; }
        public Guid FollowingId { get; set; }
    }

    public class RemoveFollowCommandHandler : IRequestHandler<RemoveFollowCommand, ApiResponse<bool>>
    {
        private readonly IFollowRepository _repository;

        public RemoveFollowCommandHandler(IFollowRepository repository)
        {
            _repository = repository;
        }

        public async Task<ApiResponse<bool>> Handle(RemoveFollowCommand request, CancellationToken cancellationToken)
        {
            var existingFollow = await _repository.GetFollowAsync(request.UserId, request.FollowingId);
            if (existingFollow == null)
            {
                return new ApiResponse<bool>(false, "You are not following this user.", 404);
            }

            await _repository.RemoveFollowAsync(existingFollow);

            return new ApiResponse<bool>(true, "Stopped following user successfully.");
        }
    }
}
