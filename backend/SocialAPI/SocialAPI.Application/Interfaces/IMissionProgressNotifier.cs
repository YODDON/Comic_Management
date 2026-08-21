using SharedKernel.Enums;

namespace SocialAPI.Interfaces;

public interface IMissionProgressNotifier
{
    Task RecordAsync(int userId, MissionType type, Guid activityId, DateTime occurredAt);
}
