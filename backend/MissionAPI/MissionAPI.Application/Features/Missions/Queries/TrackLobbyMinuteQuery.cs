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
    public class TrackLobbyMinuteQuery : IRequest<ApiResponse<IEnumerable<UserMissionDto>>>
    {
        public int UserId { get; set; }
    }

    public class TrackLobbyMinuteQueryHandler : IRequestHandler<TrackLobbyMinuteQuery, ApiResponse<IEnumerable<UserMissionDto>>>
    {
        private readonly IMissionRepository _missionRepository;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IMissionActivitySyncService _activitySyncService;

        public TrackLobbyMinuteQueryHandler(IMissionRepository missionRepository, IPublishEndpoint publishEndpoint, IMissionActivitySyncService activitySyncService)
        {
            _missionRepository = missionRepository;
            _publishEndpoint = publishEndpoint;
            _activitySyncService = activitySyncService;
        }

        public async Task<ApiResponse<IEnumerable<UserMissionDto>>> Handle(TrackLobbyMinuteQuery request, CancellationToken cancellationToken)
        {
            var missions = (await _missionRepository.GetAllMissionsAsync())
                .Where(x => x.Type == MissionType.StayInLobby).ToList();
            var updated = new List<UserMissionDto>();

            foreach (var mission in missions)
            {
                var progress = await _missionRepository.GetUserMissionAsync(request.UserId, mission.Id);
                var lastRecordedAt = progress?.UpdatedAt ?? progress?.CreatedAt;
                if (lastRecordedAt.HasValue && DateTime.UtcNow - lastRecordedAt.Value < TimeSpan.FromSeconds(50))
                    continue;

                if (progress == null)
                {
                    progress = new UserMission
                    {
                        UserId = request.UserId, MissionId = mission.Id, CurrentProgress = 0,
                        IsCompleted = false, Mission = mission, UpdatedAt = DateTime.UtcNow
                    };
                    await _missionRepository.AddUserMissionAsync(progress);
                }
                else if (!progress.IsCompleted && progress.CurrentProgress < mission.TargetCount)
                {
                    progress.CurrentProgress = Math.Min(mission.TargetCount, progress.CurrentProgress + 1);
                    progress.UpdatedAt = DateTime.UtcNow;
                    await _missionRepository.UpdateUserMissionAsync(progress);
                }

                updated.Add(new UserMissionDto
                {
                    MissionId = mission.Id, Title = mission.Title, Description = mission.Description,
                    RewardCoin = mission.RewardCoin, Type = mission.Type, TargetCount = mission.TargetCount,
                    CurrentProgress = progress.CurrentProgress, IsCompleted = progress.IsCompleted,
                    CompletedAt = progress.CompletedAt
                });
            }

            return new ApiResponse<IEnumerable<UserMissionDto>>(updated, "Lobby mission progress updated");
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
