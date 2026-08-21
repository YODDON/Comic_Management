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
    public class CreateMissionCommand : IRequest<ApiResponse<MissionDto>>
    {
        public CreateMissionDto Request { get; set; }
    }

    public class CreateMissionCommandHandler : IRequestHandler<CreateMissionCommand, ApiResponse<MissionDto>>
    {
        private readonly IMissionRepository _missionRepository;
        private readonly IPublishEndpoint _publishEndpoint;

        public CreateMissionCommandHandler(IMissionRepository missionRepository, IPublishEndpoint publishEndpoint)
        {
            _missionRepository = missionRepository;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<ApiResponse<MissionDto>> Handle(CreateMissionCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Request.Title) || request.Request.TargetCount < 1 || request.Request.RewardCoin < 0)
                return ApiResponse<MissionDto>.ErrorResponse("Invalid mission information.", 400);
            if (request.Request.EndDate.HasValue && request.Request.EndDate < request.Request.StartDate)
                return ApiResponse<MissionDto>.ErrorResponse("End date must be after start date.", 400);
            var mission = new Mission
            {
                Title = request.Request.Title,
                Description = request.Request.Description,
                RewardCoin = request.Request.RewardCoin,
                IsActive = true,
                Type = request.Request.Type,
                TargetCount = request.Request.TargetCount,
                StartDate = request.Request.StartDate,
                EndDate = request.Request.EndDate
            };

            await _missionRepository.CreateMissionAsync(mission);

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

            return new ApiResponse<MissionDto>(dto, "Mission created successfully");
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
