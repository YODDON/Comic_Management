using MassTransit;
using MissionAPI.Application.Features.Missions.Commands;
using MediatR;
using SharedKernel.Events;
using Microsoft.Extensions.Logging;

namespace MissionAPI.Consumers;

public class MissionActivityRecordedConsumer : IConsumer<MissionActivityRecordedEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<MissionActivityRecordedConsumer> _logger;

    public MissionActivityRecordedConsumer(
        IMediator mediator,
        ILogger<MissionActivityRecordedConsumer> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<MissionActivityRecordedEvent> context)
    {
        var message = context.Message;
        
        try
        {
            var result = await _mediator.Send(new RecordActivityCommand
            {
                UserId = message.UserId,
                Type = message.MissionType,
                ActivityId = message.ActivityId,
                OccurredAt = message.OccurredAt
            });
                
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
