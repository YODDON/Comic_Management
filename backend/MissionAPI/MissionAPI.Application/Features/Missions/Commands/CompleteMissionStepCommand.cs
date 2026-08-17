using System;
using SharedKernel.Events;
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
    public class CompleteMissionStepCommand : IRequest<ApiResponse<UserMissionDto>>
    {
        public int UserId { get; set; }
        public Guid MissionId { get; set; }
    }

    public class CompleteMissionStepCommandHandler : IRequestHandler<CompleteMissionStepCommand, ApiResponse<UserMissionDto>>
    {
        private readonly IMissionRepository _missionRepository;
        private readonly IPublishEndpoint _publishEndpoint;

        public CompleteMissionStepCommandHandler(IMissionRepository missionRepository, IPublishEndpoint publishEndpoint)
        {
            _missionRepository = missionRepository;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<ApiResponse<UserMissionDto>> Handle(CompleteMissionStepCommand request, CancellationToken cancellationToken)
        {
            var mission = await _missionRepository.GetMissionByIdAsync(request.MissionId);
            if (mission == null)
            {
                return new ApiResponse<UserMissionDto>(null, "Mission not found", 404);
            }

            if (!mission.IsActive)
            {
                return new ApiResponse<UserMissionDto>(null, "Mission is not currently active", 400);
            }

            var userMission = await _missionRepository.GetUserMissionAsync(request.UserId, request.MissionId);
            if (userMission == null)
            {
                return ApiResponse<UserMissionDto>.ErrorResponse("Bạn chưa hoàn thành nhiệm vụ.", 400);
            }

            if (userMission.IsCompleted)
            {
                return new ApiResponse<UserMissionDto>(new UserMissionDto
                {
                    MissionId = mission.Id, Title = mission.Title, Description = mission.Description,
                    RewardCoin = mission.RewardCoin, Type = mission.Type, TargetCount = mission.TargetCount,
                    CurrentProgress = userMission.CurrentProgress, IsCompleted = true,
                    CompletedAt = userMission.CompletedAt
                }, "Mission is already completed.");
            }

            if (userMission.CurrentProgress < mission.TargetCount)
            {
                return ApiResponse<UserMissionDto>.ErrorResponse("Bạn chưa hoàn thành nhiệm vụ.", 400);
            }

            userMission.CurrentProgress = mission.TargetCount;
            userMission.IsCompleted = true;
            userMission.CompletedAt = DateTime.UtcNow;
            userMission.UpdatedAt = DateTime.UtcNow;
            
            await _missionRepository.UpdateUserMissionAsync(userMission);

            await _publishEndpoint.Publish(new MissionRewardGrantedEvent
            {
                UserId = request.UserId,
                CoinAmount = mission.RewardCoin,
                ReferenceId = CreateRewardReference(request.UserId, mission.Id),
                Description = $"Mission reward: {mission.Title}",
                OccurredAt = DateTime.UtcNow
            });

            await _missionRepository.SaveChangesAsync();

            var dto = new UserMissionDto
            {
                MissionId = userMission.MissionId,
                Title = mission.Title,
                Description = mission.Description,
                RewardCoin = mission.RewardCoin,
                Type = mission.Type,
                TargetCount = mission.TargetCount,
                CurrentProgress = userMission.CurrentProgress,
                IsCompleted = userMission.IsCompleted,
                CompletedAt = userMission.CompletedAt
            };

            return new ApiResponse<UserMissionDto>(dto, $"Hoàn thành nhiệm vụ và nhận {mission.RewardCoin} Dâu.");
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
