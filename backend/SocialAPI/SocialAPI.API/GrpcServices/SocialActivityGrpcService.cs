using Grpc.Core;
using MediatR;
using SocialAPI.Application.Features.SocialActivities.Queries;
using SocialAPI.Protos;
using SocialAPI.Interfaces;

namespace SocialAPI.GrpcServices;

public class SocialActivityGrpcService : SocialActivity.SocialActivityBase
{
    private readonly IMediator _mediator;

    public SocialActivityGrpcService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<GetUserActivitiesResponse> GetUserActivities(
        GetUserActivitiesRequest request,
        ServerCallContext context)
    {
        if (request.UserId <= 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "UserId is invalid."));
        }

        var activities = await _mediator.Send(new GetUserActivitiesQuery { NumericUserId = request.UserId }, context.CancellationToken);

        var response = new GetUserActivitiesResponse();
        response.ReadChapters.AddRange(activities.ReadChapters.Select(item => new ActivitySnapshot
        {
            ActivityId = item.ActivityId.ToString(),
            OccurredAtUnixSeconds = new DateTimeOffset(item.OccurredAt.ToUniversalTime()).ToUnixTimeSeconds()
        }));
        response.Comments.AddRange(activities.Comments.Select(item => new ActivitySnapshot
        {
            ActivityId = item.ActivityId.ToString(),
            OccurredAtUnixSeconds = new DateTimeOffset(item.OccurredAt.ToUniversalTime()).ToUnixTimeSeconds()
        }));
        return response;
    }
}
