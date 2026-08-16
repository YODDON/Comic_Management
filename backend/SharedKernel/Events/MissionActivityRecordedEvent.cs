using System;
using SharedKernel.Enums;

namespace SharedKernel.Events;

public record MissionActivityRecordedEvent
{
    public int UserId { get; init; }
    public MissionType MissionType { get; init; }
    public Guid ActivityId { get; init; }
    public DateTime OccurredAt { get; init; }
}
