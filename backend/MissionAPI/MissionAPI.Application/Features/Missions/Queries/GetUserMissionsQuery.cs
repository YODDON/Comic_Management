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
    public class GetUserMissionsQuery : IRequest<ApiResponse<IEnumerable<UserMissionDto>>>
    {
        public int UserId { get; set; }
    }

    public class GetUserMissionsQueryHandler : IRequestHandler<GetUserMissionsQuery, ApiResponse<IEnumerable<UserMissionDto>>>
    {
        private readonly IMissionRepository _missionRepository;
        private readonly IPublishEndpoint _publishEndpoint;

        public GetUserMissionsQueryHandler(IMissionRepository missionRepository, IPublishEndpoint publishEndpoint)
        {
            _missionRepository = missionRepository;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<ApiResponse<IEnumerable<UserMissionDto>>> Handle(GetUserMissionsQuery request, CancellationToken cancellationToken)
        {
            var userMissions = await _missionRepository.GetUserMissionsAsync(request.UserId);
            var progress = userMissions.ToDictionary(x => x.MissionId);
            var missions = await _missionRepository.GetAllMissionsAsync();
            var dtos = missions.Select(m =>
            {
                progress.TryGetValue(m.Id, out var um);
                return new UserMissionDto
                {
                    MissionId = m.Id, Title = m.Title, Description = m.Description,
                    RewardCoin = m.RewardCoin, Type = m.Type, TargetCount = m.TargetCount,
                    CurrentProgress = um?.CurrentProgress ?? 0,
                    IsCompleted = um?.IsCompleted ?? false,
                    CompletedAt = um?.CompletedAt
                };
            }).Where(mission => !mission.IsCompleted);
            return new ApiResponse<IEnumerable<UserMissionDto>>(dtos, "User missions retrieved successfully");
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
