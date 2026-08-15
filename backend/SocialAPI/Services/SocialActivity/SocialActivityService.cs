using SocialAPI.DTOs;
using SocialAPI.Interfaces;

namespace SocialAPI.Services;

public class SocialActivityService : ISocialActivityService
{
    private readonly ISocialActivityRepository _repository;

    public SocialActivityService(ISocialActivityRepository repository) => _repository = repository;

    public Task<SocialActivitySnapshotDto> GetUserActivitiesAsync(
        int numericUserId, CancellationToken cancellationToken)
    {
        var userId = new Guid(
            System.Security.Cryptography.MD5.HashData(BitConverter.GetBytes(numericUserId)));
        return _repository.GetUserActivitiesAsync(userId, cancellationToken);
    }
}
