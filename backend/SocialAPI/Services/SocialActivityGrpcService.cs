using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using SocialAPI.Data;
using SocialAPI.Protos;

namespace SocialAPI.Services;

public class SocialActivityGrpcService : SocialActivity.SocialActivityBase
{
    private readonly SocialDbContext _context;

    public SocialActivityGrpcService(SocialDbContext context)
    {
        _context = context;
    }

    public override async Task<GetUserActivitiesResponse> GetUserActivities(
        GetUserActivitiesRequest request,
        ServerCallContext context)
    {
        if (request.UserId <= 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "UserId is invalid."));
        }

        var socialUserId = new Guid(
            System.Security.Cryptography.MD5.HashData(BitConverter.GetBytes(request.UserId)));
        var readChapters = await _context.ReadingHistories
            .AsNoTracking()
            .Where(item => item.UserId == socialUserId && item.ChapterId != Guid.Empty)
            .Select(item => new { item.ChapterId, item.ReadAt })
            .ToListAsync(context.CancellationToken);
        var comments = await _context.Comments
            .AsNoTracking()
            .Where(item => item.UserId == socialUserId)
            .Select(item => new { item.Id, item.CreatedAt })
            .ToListAsync(context.CancellationToken);

        var response = new GetUserActivitiesResponse();
        response.ReadChapters.AddRange(readChapters.Select(item => new ActivitySnapshot
        {
            ActivityId = item.ChapterId.ToString(),
            OccurredAtUnixSeconds = new DateTimeOffset(item.ReadAt.ToUniversalTime()).ToUnixTimeSeconds()
        }));
        response.Comments.AddRange(comments.Select(item => new ActivitySnapshot
        {
            ActivityId = item.Id.ToString(),
            OccurredAtUnixSeconds = new DateTimeOffset(item.CreatedAt.ToUniversalTime()).ToUnixTimeSeconds()
        }));
        return response;
    }
}
