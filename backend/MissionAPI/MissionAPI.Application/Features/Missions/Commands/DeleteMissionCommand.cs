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
    public class DeleteMissionCommand : IRequest<ApiResponse<object>>
    {
        public Guid Id { get; set; }
    }

    public class DeleteMissionCommandHandler : IRequestHandler<DeleteMissionCommand, ApiResponse<object>>
    {
        private readonly IMissionRepository _missionRepository;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IMissionActivitySyncService _activitySyncService;

        public DeleteMissionCommandHandler(IMissionRepository missionRepository, IPublishEndpoint publishEndpoint, IMissionActivitySyncService activitySyncService)
        {
            _missionRepository = missionRepository;
            _publishEndpoint = publishEndpoint;
            _activitySyncService = activitySyncService;
        }

        public async Task<ApiResponse<object>> Handle(DeleteMissionCommand request, CancellationToken cancellationToken)
        {
            var mission = await _missionRepository.GetMissionByIdAsync(request.Id);
            if (mission == null) return ApiResponse<object>.ErrorResponse("Mission not found", 404);

            // Soft-delete keeps completed mission history and prevents reward records from being orphaned.
            mission.IsActive = false;
            mission.UpdatedAt = DateTime.UtcNow;
            await _missionRepository.UpdateMissionAsync(mission);
            return new ApiResponse<object>(new { mission.Id }, "Mission deleted successfully");
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
