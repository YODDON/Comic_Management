using SocialAPI.DTOs;

namespace SocialAPI.Interfaces;

public interface ISocialActivityService
{
    Task<SocialActivitySnapshotDto> GetUserActivitiesAsync(int numericUserId, CancellationToken cancellationToken);
}
