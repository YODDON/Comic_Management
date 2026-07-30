using SharedKernel.Enums;

namespace ChapterAPI.Interfaces;

public interface IMissionProgressNotifier
{
    Task RecordAsync(int userId, MissionType type, Guid activityId, DateTime occurredAt);
}
