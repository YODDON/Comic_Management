using MassTransit;
using SharedKernel.Enums;
using SharedKernel.Events;
using SocialAPI.Interfaces;

namespace SocialAPI.Services;

public class MissionProgressNotifier : IMissionProgressNotifier
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<MissionProgressNotifier> _logger;

    public MissionProgressNotifier(
        IPublishEndpoint publishEndpoint,
        ILogger<MissionProgressNotifier> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task RecordAsync(int userId, MissionType type, Guid activityId, DateTime occurredAt)
    {
        try
        {
            await _publishEndpoint.Publish(new MissionActivityRecordedEvent
            {
                UserId = userId,
                MissionType = type,
                ActivityId = activityId,
                OccurredAt = occurredAt
            });
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception,
                "Could not publish {MissionType} mission activity {ActivityId} for user {UserId}.",
                type, activityId, userId);
        }
    }
}
