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

namespace MissionAPI.Application.Features.Missions.Queries
{
    public class GetAllMissionsForAdminQuery : IRequest<ApiResponse<IEnumerable<MissionDto>>>
    {
    }

    public class GetAllMissionsForAdminQueryHandler : IRequestHandler<GetAllMissionsForAdminQuery, ApiResponse<IEnumerable<MissionDto>>>
    {
        private readonly IMissionRepository _missionRepository;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IMissionActivitySyncService _activitySyncService;

        public GetAllMissionsForAdminQueryHandler(IMissionRepository missionRepository, IPublishEndpoint publishEndpoint, IMissionActivitySyncService activitySyncService)
        {
            _missionRepository = missionRepository;
            _publishEndpoint = publishEndpoint;
            _activitySyncService = activitySyncService;
        }

        public async Task<ApiResponse<IEnumerable<MissionDto>>> Handle(GetAllMissionsForAdminQuery request, CancellationToken cancellationToken)
        {
            var missions = await _missionRepository.GetAllMissionsForAdminAsync();
            var dtos = missions.Select(ToMissionDto);
            return new ApiResponse<IEnumerable<MissionDto>>(dtos, "Missions retrieved successfully");
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
