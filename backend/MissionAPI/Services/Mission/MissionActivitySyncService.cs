using ChapterAPI.Protos;
using Grpc.Core;
using MissionAPI.Interfaces;
using SharedKernel.Enums;
using SocialAPI.Protos;

namespace MissionAPI.Services;

public class MissionActivitySyncService : IMissionActivitySyncService
{
    private readonly IMissionRepository _repository;
    private readonly ChapterGrpc.ChapterGrpcClient _chapterClient;
    private readonly SocialActivity.SocialActivityClient _socialClient;
    private readonly ILogger<MissionActivitySyncService> _logger;

    public MissionActivitySyncService(
        IMissionRepository repository,
        ChapterGrpc.ChapterGrpcClient chapterClient,
        SocialActivity.SocialActivityClient socialClient,
        ILogger<MissionActivitySyncService> logger)
    {
        _repository = repository;
        _chapterClient = chapterClient;
        _socialClient = socialClient;
        _logger = logger;
    }

    public async Task SyncAsync(int userId, CancellationToken cancellationToken = default)
    {
        await SyncPurchasesAsync(userId, cancellationToken);
        await SyncSocialActivitiesAsync(userId, cancellationToken);
    }

    private async Task SyncPurchasesAsync(int userId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _chapterClient.GetUserPurchaseActivitiesAsync(
                new GetUserPurchaseActivitiesRequest { UserId = userId },
                cancellationToken: cancellationToken);
            foreach (var activity in response.Activities)
            {
                if (!Guid.TryParse(activity.ChapterId, out var activityId)) continue;
                await _repository.RecordActivityAsync(
                    userId,
                    MissionType.PurchaseChapter,
                    activityId,
                    FromUnixTime(activity.PurchasedAtUnixSeconds));
            }
        }
        catch (RpcException exception)
        {
            _logger.LogWarning(exception, "Could not synchronize chapter purchases for user {UserId}.", userId);
        }
    }

    private async Task SyncSocialActivitiesAsync(int userId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _socialClient.GetUserActivitiesAsync(
                new GetUserActivitiesRequest { UserId = userId },
                cancellationToken: cancellationToken);
            foreach (var activity in response.ReadChapters)
            {
                if (!Guid.TryParse(activity.ActivityId, out var activityId)) continue;
                await _repository.RecordActivityAsync(
                    userId,
                    MissionType.ReadChapter,
                    activityId,
                    FromUnixTime(activity.OccurredAtUnixSeconds));
            }

            foreach (var activity in response.Comments)
            {
                if (!Guid.TryParse(activity.ActivityId, out var activityId)) continue;
                await _repository.RecordActivityAsync(
                    userId,
                    MissionType.LeaveComment,
                    activityId,
                    FromUnixTime(activity.OccurredAtUnixSeconds));
            }
        }
        catch (RpcException exception)
        {
            _logger.LogWarning(exception, "Could not synchronize social activities for user {UserId}.", userId);
        }
    }

    private static DateTime FromUnixTime(long value) => value > 0
        ? DateTimeOffset.FromUnixTimeSeconds(value).UtcDateTime
        : DateTime.UtcNow;
}
