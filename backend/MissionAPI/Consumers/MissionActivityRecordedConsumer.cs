using MassTransit;
using MissionAPI.Interfaces;
using SharedKernel.Events;

namespace MissionAPI.Consumers;

public class MissionActivityRecordedConsumer : IConsumer<MissionActivityRecordedEvent>
{
    private readonly IMissionService _missionService;
    private readonly ILogger<MissionActivityRecordedConsumer> _logger;

    public MissionActivityRecordedConsumer(
        IMissionService missionService,
        ILogger<MissionActivityRecordedConsumer> logger)
    {
        _missionService = missionService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<MissionActivityRecordedEvent> context)
    {
        var message = context.Message;
        
        try
        {
            var result = await _missionService.RecordActivityAsync(
                message.UserId,
                message.MissionType,
                message.ActivityId,
                message.OccurredAt);
                
            if (!result.Success)
            {
                _logger.LogWarning("Failed to record mission activity from event: {Message}", result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MissionActivityRecordedEvent for UserId: {UserId}, ActivityId: {ActivityId}", message.UserId, message.ActivityId);
            throw; // Re-throw to allow MassTransit to retry or move to dead-letter queue
        }
    }
}
