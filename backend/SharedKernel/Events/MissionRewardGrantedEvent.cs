using System;

namespace SharedKernel.Events;

public record MissionRewardGrantedEvent
{
    public int UserId { get; init; }
    public decimal CoinAmount { get; init; }
    public Guid ReferenceId { get; init; }
    public string? Description { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
