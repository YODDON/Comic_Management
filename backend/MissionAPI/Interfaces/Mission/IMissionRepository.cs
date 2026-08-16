using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MissionAPI.Entities;
using SharedKernel.Enums;

namespace MissionAPI.Interfaces
{
    public interface IMissionRepository
    {
        Task<IEnumerable<Mission>> GetAllMissionsAsync();
        Task<IEnumerable<Mission>> GetAllMissionsForAdminAsync();
        Task<Mission?> GetMissionByIdAsync(Guid id);
        Task<Mission> CreateMissionAsync(Mission mission);
        Task UpdateMissionAsync(Mission mission);
        
        Task<IEnumerable<UserMission>> GetUserMissionsAsync(int userId);
        Task<UserMission?> GetUserMissionAsync(int userId, Guid missionId);
        Task<UserMission> AddUserMissionAsync(UserMission userMission);
        Task UpdateUserMissionAsync(UserMission userMission);
        Task<IReadOnlyList<UserMission>> RecordActivityAsync(
            int userId,
            MissionType type,
            Guid activityId,
            DateTime occurredAt);
            
        Task SaveChangesAsync();
    }
}
