using SocialAPI.DTOs;

namespace SocialAPI.Interfaces;

public interface ISocialActivityRepository
{
    Task<SocialActivitySnapshotDto> GetUserActivitiesAsync(Guid userId, CancellationToken cancellationToken);
}
