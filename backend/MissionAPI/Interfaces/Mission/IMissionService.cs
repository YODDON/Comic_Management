using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MissionAPI.DTOs;
using SharedKernel.Enums;
using SharedKernel.Responses;

namespace MissionAPI.Interfaces
{
    public interface IMissionService
    {
        Task<ApiResponse<IEnumerable<MissionDto>>> GetAllMissionsAsync();
        Task<ApiResponse<IEnumerable<MissionDto>>> GetAllMissionsForAdminAsync();
        Task<ApiResponse<IEnumerable<UserMissionDto>>> GetUserMissionsAsync(int userId);
        Task<ApiResponse<MissionDto>> CreateMissionAsync(CreateMissionDto request);
        Task<ApiResponse<MissionDto>> UpdateMissionAsync(Guid id, UpdateMissionDto request);
        Task<ApiResponse<object>> DeleteMissionAsync(Guid id);
        Task<ApiResponse<UserMissionDto>> CompleteMissionStepAsync(int userId, Guid missionId);
        Task<ApiResponse<IEnumerable<UserMissionDto>>> TrackLobbyMinuteAsync(int userId);
        Task<ApiResponse<IEnumerable<UserMissionDto>>> RecordActivityAsync(
            int userId,
            MissionType type,
            Guid activityId,
            DateTime? occurredAt = null);
    }
}
