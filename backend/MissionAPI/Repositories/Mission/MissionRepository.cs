using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using Microsoft.EntityFrameworkCore;
using MissionAPI.Data;
using MissionAPI.Entities;
using MissionAPI.Interfaces;
using SharedKernel.Enums;

namespace MissionAPI.Repositories
{
    public class MissionRepository : IMissionRepository
    {
        private readonly MissionDbContext _context;

        public MissionRepository(MissionDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Mission>> GetAllMissionsAsync()
        {
            var now = DateTime.UtcNow;
            return await _context.Missions
                .Where(x => x.IsActive && x.StartDate <= now && (!x.EndDate.HasValue || x.EndDate >= now))
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Mission>> GetAllMissionsForAdminAsync() =>
            await _context.Missions.OrderByDescending(x => x.CreatedAt).ToListAsync();

        public async Task<Mission?> GetMissionByIdAsync(Guid id)
        {
            return await _context.Missions.FindAsync(id);
        }

        public async Task<Mission> CreateMissionAsync(Mission mission)
        {
            _context.Missions.Add(mission);
            await _context.SaveChangesAsync();
            return mission;
        }

        public async Task UpdateMissionAsync(Mission mission)
        {
            _context.Missions.Update(mission);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<UserMission>> GetUserMissionsAsync(int userId)
        {
            return await _context.UserMissions
                .Include(um => um.Mission)
                .Where(um => um.UserId == userId)
                .ToListAsync();
        }

        public async Task<UserMission?> GetUserMissionAsync(int userId, Guid missionId)
        {
            return await _context.UserMissions
                .Include(um => um.Mission)
                .FirstOrDefaultAsync(um => um.UserId == userId && um.MissionId == missionId);
        }

        public async Task<UserMission> AddUserMissionAsync(UserMission userMission)
        {
            _context.UserMissions.Add(userMission);
            await _context.SaveChangesAsync();
            return userMission;
        }

        public Task UpdateUserMissionAsync(UserMission userMission)
        {
            _context.UserMissions.Update(userMission);
            return Task.CompletedTask;
        }
        
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<UserMission>> RecordActivityAsync(
            int userId,
            MissionType type,
            Guid activityId,
            DateTime occurredAt)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var now = DateTime.UtcNow;
            var missions = await _context.Missions
                .Where(mission => mission.IsActive
                    && mission.Type == type
                    && mission.StartDate <= occurredAt
                    && (!mission.EndDate.HasValue || mission.EndDate >= occurredAt))
                .ToListAsync();
            var updated = new List<UserMission>();

            foreach (var mission in missions)
            {
                var progress = await _context.UserMissions
                    .FirstOrDefaultAsync(item => item.UserId == userId && item.MissionId == mission.Id);
                if (progress?.IsCompleted == true || progress?.CurrentProgress >= mission.TargetCount)
                {
                    if (progress != null)
                    {
                        progress.Mission = mission;
                        updated.Add(progress);
                    }
                    continue;
                }

                var alreadyRecorded = await _context.MissionActivities.AnyAsync(item =>
                    item.UserId == userId
                    && item.MissionId == mission.Id
                    && item.ActivityId == activityId);
                if (alreadyRecorded)
                {
                    if (progress != null)
                    {
                        progress.Mission = mission;
                        updated.Add(progress);
                    }
                    continue;
                }

                _context.MissionActivities.Add(new MissionActivity
                {
                    UserId = userId,
                    MissionId = mission.Id,
                    ActivityId = activityId,
                    OccurredAt = occurredAt
                });

                if (progress == null)
                {
                    progress = new UserMission
                    {
                        UserId = userId,
                        MissionId = mission.Id,
                        CurrentProgress = 1,
                        IsCompleted = false,
                        UpdatedAt = now
                    };
                    _context.UserMissions.Add(progress);
                }
                else
                {
                    progress.CurrentProgress = Math.Min(mission.TargetCount, progress.CurrentProgress + 1);
                    progress.UpdatedAt = now;
                }

                progress.Mission = mission;
                updated.Add(progress);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return updated;
        }
    }
}
