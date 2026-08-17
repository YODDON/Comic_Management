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
    public class UpdateMissionCommand : IRequest<ApiResponse<MissionDto>>
    {
        public Guid Id { get; set; }
        public UpdateMissionDto Request { get; set; }
    }

    public class UpdateMissionCommandHandler : IRequestHandler<UpdateMissionCommand, ApiResponse<MissionDto>>
    {
        private readonly IMissionRepository _missionRepository;
        private readonly IPublishEndpoint _publishEndpoint;

        public UpdateMissionCommandHandler(IMissionRepository missionRepository, IPublishEndpoint publishEndpoint)
        {
            _missionRepository = missionRepository;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<ApiResponse<MissionDto>> Handle(UpdateMissionCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Request.Title) || request.Request.TargetCount < 1 || request.Request.RewardCoin < 0)
                return ApiResponse<MissionDto>.ErrorResponse("Invalid mission information.", 400);
            if (request.Request.EndDate.HasValue && request.Request.EndDate < request.Request.StartDate)
                return ApiResponse<MissionDto>.ErrorResponse("End date must be after start date.", 400);
            var mission = await _missionRepository.GetMissionByIdAsync(request.Id);
            if (mission == null)
            {
                return new ApiResponse<MissionDto>(null, "Mission not found", 404);
            }

            mission.Title = request.Request.Title;
            mission.Description = request.Request.Description;
            mission.RewardCoin = request.Request.RewardCoin;
            mission.IsActive = request.Request.IsActive;
            mission.Type = request.Request.Type;
            mission.TargetCount = request.Request.TargetCount;
            mission.StartDate = request.Request.StartDate;
            mission.EndDate = request.Request.EndDate;

            await _missionRepository.UpdateMissionAsync(mission);

            var dto = new MissionDto
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

            return new ApiResponse<MissionDto>(dto, "Mission updated successfully");
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
