using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MissionAPI.DTOs;
using MissionAPI.Entities;
using MissionAPI.Interfaces;
using SharedKernel.Enums;
using SharedKernel.Responses;
using SharedKernel.Events;
using MassTransit;

namespace MissionAPI.Services
{
    public class MissionService : IMissionService
    {
        private readonly IMissionRepository _missionRepository;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IMissionActivitySyncService _activitySyncService;

        public MissionService(
            IMissionRepository missionRepository,
            IPublishEndpoint publishEndpoint,
            IMissionActivitySyncService activitySyncService)
        {
            _missionRepository = missionRepository;
            _publishEndpoint = publishEndpoint;
            _activitySyncService = activitySyncService;
        }

        public async Task<ApiResponse<IEnumerable<MissionDto>>> GetAllMissionsAsync()
        {
            var missions = await _missionRepository.GetAllMissionsAsync();
            var dtos = missions.Select(m => new MissionDto
            {
                Id = m.Id,
                Title = m.Title,
                Description = m.Description,
                RewardCoin = m.RewardCoin,
                IsActive = m.IsActive,
                Type = m.Type,
                TargetCount = m.TargetCount,
                StartDate = m.StartDate,
                EndDate = m.EndDate
            });
            return new ApiResponse<IEnumerable<MissionDto>>(dtos, "Missions retrieved successfully");
        }

        public async Task<ApiResponse<IEnumerable<MissionDto>>> GetAllMissionsForAdminAsync()
        {
            var missions = await _missionRepository.GetAllMissionsForAdminAsync();
            var dtos = missions.Select(ToMissionDto);
            return new ApiResponse<IEnumerable<MissionDto>>(dtos, "Missions retrieved successfully");
        }

        public async Task<ApiResponse<IEnumerable<UserMissionDto>>> GetUserMissionsAsync(int userId)
        {
            await _activitySyncService.SyncAsync(userId);
            var userMissions = await _missionRepository.GetUserMissionsAsync(userId);
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

        public async Task<ApiResponse<IEnumerable<UserMissionDto>>> RecordActivityAsync(
            int userId,
            MissionType type,
            Guid activityId,
            DateTime? occurredAt = null)
        {
            if (userId <= 0 || activityId == Guid.Empty)
            {
                return ApiResponse<IEnumerable<UserMissionDto>>.ErrorResponse("Invalid mission activity.", 400);
            }

            var updated = await _missionRepository.RecordActivityAsync(
                userId,
                type,
                activityId,
                occurredAt?.ToUniversalTime() ?? DateTime.UtcNow);
            return new ApiResponse<IEnumerable<UserMissionDto>>(
                updated.Select(ToUserMissionDto),
                "Mission progress updated.");
        }

        public async Task<ApiResponse<MissionDto>> CreateMissionAsync(CreateMissionDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Title) || request.TargetCount < 1 || request.RewardCoin < 0)
                return ApiResponse<MissionDto>.ErrorResponse("Invalid mission information.", 400);
            if (request.EndDate.HasValue && request.EndDate < request.StartDate)
                return ApiResponse<MissionDto>.ErrorResponse("End date must be after start date.", 400);
            var mission = new Mission
            {
                Title = request.Title,
                Description = request.Description,
                RewardCoin = request.RewardCoin,
                IsActive = true,
                Type = request.Type,
                TargetCount = request.TargetCount,
                StartDate = request.StartDate,
                EndDate = request.EndDate
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

        public async Task<ApiResponse<MissionDto>> UpdateMissionAsync(Guid id, UpdateMissionDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Title) || request.TargetCount < 1 || request.RewardCoin < 0)
                return ApiResponse<MissionDto>.ErrorResponse("Invalid mission information.", 400);
            if (request.EndDate.HasValue && request.EndDate < request.StartDate)
                return ApiResponse<MissionDto>.ErrorResponse("End date must be after start date.", 400);
            var mission = await _missionRepository.GetMissionByIdAsync(id);
            if (mission == null)
            {
                return new ApiResponse<MissionDto>(null, "Mission not found", 404);
            }

            mission.Title = request.Title;
            mission.Description = request.Description;
            mission.RewardCoin = request.RewardCoin;
            mission.IsActive = request.IsActive;
            mission.Type = request.Type;
            mission.TargetCount = request.TargetCount;
            mission.StartDate = request.StartDate;
            mission.EndDate = request.EndDate;

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

        public async Task<ApiResponse<object>> DeleteMissionAsync(Guid id)
        {
            var mission = await _missionRepository.GetMissionByIdAsync(id);
            if (mission == null) return ApiResponse<object>.ErrorResponse("Mission not found", 404);

            // Soft-delete keeps completed mission history and prevents reward records from being orphaned.
            mission.IsActive = false;
            mission.UpdatedAt = DateTime.UtcNow;
            await _missionRepository.UpdateMissionAsync(mission);
            return new ApiResponse<object>(new { mission.Id }, "Mission deleted successfully");
        }

        public async Task<ApiResponse<UserMissionDto>> CompleteMissionStepAsync(int userId, Guid missionId)
        {
            var mission = await _missionRepository.GetMissionByIdAsync(missionId);
            if (mission == null)
            {
                return new ApiResponse<UserMissionDto>(null, "Mission not found", 404);
            }

            if (!mission.IsActive)
            {
                return new ApiResponse<UserMissionDto>(null, "Mission is not currently active", 400);
            }

            var userMission = await _missionRepository.GetUserMissionAsync(userId, missionId);
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
                UserId = userId,
                CoinAmount = mission.RewardCoin,
                ReferenceId = CreateRewardReference(userId, mission.Id),
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

        public async Task<ApiResponse<IEnumerable<UserMissionDto>>> TrackLobbyMinuteAsync(int userId)
        {
            var missions = (await _missionRepository.GetAllMissionsAsync())
                .Where(x => x.Type == MissionType.StayInLobby).ToList();
            var updated = new List<UserMissionDto>();

            foreach (var mission in missions)
            {
                var progress = await _missionRepository.GetUserMissionAsync(userId, mission.Id);
                var lastRecordedAt = progress?.UpdatedAt ?? progress?.CreatedAt;
                if (lastRecordedAt.HasValue && DateTime.UtcNow - lastRecordedAt.Value < TimeSpan.FromSeconds(50))
                    continue;

                if (progress == null)
                {
                    progress = new UserMission
                    {
                        UserId = userId, MissionId = mission.Id, CurrentProgress = 0,
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
