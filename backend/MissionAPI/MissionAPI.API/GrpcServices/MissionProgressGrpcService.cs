using Grpc.Core;
using MissionAPI.Interfaces;
using MissionAPI.Protos;
using SharedKernel.Enums;

namespace MissionAPI.GrpcServices;

public class MissionProgressGrpcService : MissionProgress.MissionProgressBase
{
    private readonly IMissionService _missionService;

    public MissionProgressGrpcService(IMissionService missionService)
    {
        _missionService = missionService;
    }

    public override async Task<RecordMissionActivityResponse> RecordActivity(
        RecordMissionActivityRequest request,
        ServerCallContext context)
    {
        if (request.UserId <= 0
            || !Enum.TryParse<MissionType>(request.MissionType, true, out var type)
            || !Guid.TryParse(request.ActivityId, out var activityId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid mission activity."));
        }

        DateTime? occurredAt = request.OccurredAtUnixSeconds > 0
            ? DateTimeOffset.FromUnixTimeSeconds(request.OccurredAtUnixSeconds).UtcDateTime
            : null;
        var result = await _missionService.RecordActivityAsync(
            request.UserId,
            type,
            activityId,
            occurredAt);

        return new RecordMissionActivityResponse
        {
            Success = result.Success,
            Message = result.Message ?? string.Empty
        };
    }
}
