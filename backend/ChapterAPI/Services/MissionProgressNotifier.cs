using ChapterAPI.Interfaces;
using Grpc.Core;
using MissionAPI.Protos;
using SharedKernel.Enums;

namespace ChapterAPI.Services;

public class MissionProgressNotifier : IMissionProgressNotifier
{
    private readonly MissionProgress.MissionProgressClient _client;
    private readonly ILogger<MissionProgressNotifier> _logger;

    public MissionProgressNotifier(
        MissionProgress.MissionProgressClient client,
        ILogger<MissionProgressNotifier> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task RecordAsync(int userId, MissionType type, Guid activityId, DateTime occurredAt)
    {
        try
        {
            await _client.RecordActivityAsync(new RecordMissionActivityRequest
            {
                UserId = userId,
                MissionType = type.ToString(),
                ActivityId = activityId.ToString(),
                OccurredAtUnixSeconds = new DateTimeOffset(occurredAt.ToUniversalTime()).ToUnixTimeSeconds()
            });
        }
        catch (RpcException exception)
        {
            _logger.LogWarning(exception,
                "Could not record {MissionType} mission activity {ActivityId} for user {UserId}.",
                type, activityId, userId);
        }
    }
}
