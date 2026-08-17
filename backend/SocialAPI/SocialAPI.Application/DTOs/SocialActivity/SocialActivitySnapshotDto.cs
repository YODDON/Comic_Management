namespace SocialAPI.DTOs;

public record ActivityItemDto(Guid ActivityId, DateTime OccurredAt);

public class SocialActivitySnapshotDto
{
    public List<ActivityItemDto> ReadChapters { get; set; } = [];
    public List<ActivityItemDto> Comments { get; set; } = [];
}
