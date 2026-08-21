using Grpc.Core;
using MediatR;
using MissionAPI.Application.Features.Missions.Queries;
using MissionAPI.Application.Features.Missions.Commands;
using MissionAPI.Application.Features.Notifications.Queries;
using MissionAPI.Application.Features.Notifications.Commands;
using MissionAPI.Protos;
using SharedKernel.Enums;

namespace MissionAPI.GrpcServices;

public class MissionProgressGrpcService : MissionProgress.MissionProgressBase
{
    private readonly IMediator _mediator;

    public MissionProgressGrpcService(IMediator mediator)
    {
        _mediator = mediator;
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
        var result = await _mediator.Send(new RecordActivityCommand
        {
            UserId = request.UserId,
            Type = type,
            ActivityId = activityId,
            OccurredAt = occurredAt
        });

        return new RecordMissionActivityResponse
        {
            Success = result.Success,
            Message = result.Message ?? string.Empty
        };
    }
}
