using System;
using MassTransit;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SharedKernel.Responses;
using SharedKernel.Enums;
using MissionAPI.DTOs;
using MissionAPI.Entities;
using MissionAPI.Interfaces;

namespace MissionAPI.Application.Features.Missions.Commands
{
    public class RecordActivityCommand : IRequest<ApiResponse<IEnumerable<UserMissionDto>>>
    {
        public int UserId { get; set; }
        public MissionType Type { get; set; }
        public Guid ActivityId { get; set; }
        public DateTime? OccurredAt { get; set; }
    }

    public class RecordActivityCommandHandler : IRequestHandler<RecordActivityCommand, ApiResponse<IEnumerable<UserMissionDto>>>
    {
        private readonly IMissionRepository _missionRepository;
        private readonly IPublishEndpoint _publishEndpoint;

        public RecordActivityCommandHandler(IMissionRepository missionRepository, IPublishEndpoint publishEndpoint)
        {
            _missionRepository = missionRepository;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<ApiResponse<IEnumerable<UserMissionDto>>> Handle(RecordActivityCommand request, CancellationToken cancellationToken)
        {
            if (request.UserId <= 0 || request.ActivityId == Guid.Empty)
            {
                return ApiResponse<IEnumerable<UserMissionDto>>.ErrorResponse("Invalid mission activity.", 400);
            }

            var updated = await _missionRepository.RecordActivityAsync(
                request.UserId,
                request.Type,
                request.ActivityId,
                request.OccurredAt?.ToUniversalTime() ?? DateTime.UtcNow);
            return new ApiResponse<IEnumerable<UserMissionDto>>(
                updated.Select(ToUserMissionDto),
                "Mission progress updated.");
        }

        private static Guid CreateRewardReference(int userId, Guid missionId)
        {
            var input = missionId.ToByteArray().Concat(BitConverter.GetBytes(userId)).ToArray();
            return new Guid(System.Security.Cryptography.MD5.HashData(input));
        }

        private static MissionDto ToMissionDto(Mission mission) => new()
        {
            Id = mission.Id,
            Title = mission.Title,
            Description = mission.Description,
            RewardCoin = mission.RewardCoin,
            IsActive = mission.IsActive,
            Type = mission.Type,
            TargetCount = mission.TargetCount,
            StartDate = mission.StartDate,
            EndDate = mission.EndDate
        };

        private static UserMissionDto ToUserMissionDto(UserMission progress) => new()
        {
            MissionId = progress.MissionId,
            Title = progress.Mission.Title,
            Description = progress.Mission.Description,
            RewardCoin = progress.Mission.RewardCoin,
            Type = progress.Mission.Type,
            TargetCount = progress.Mission.TargetCount,
            CurrentProgress = progress.CurrentProgress,
            IsCompleted = progress.IsCompleted,
            CompletedAt = progress.CompletedAt
        };
    }
}
