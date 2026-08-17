using System;
using System.Threading.Tasks;
using ChapterAPI.Interfaces;
using ChapterAPI.Protos;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using SharedKernel.Enums;

namespace ChapterAPI.GrpcServices
{
    public class ChapterGrpcService : ChapterGrpc.ChapterGrpcBase
    {
        private readonly IChapterService _chapterService;
        private readonly ILogger<ChapterGrpcService> _logger;

        public ChapterGrpcService(
            IChapterService chapterService,
            ILogger<ChapterGrpcService> logger)
        {
            _chapterService = chapterService;
            _logger = logger;
        }

        public override async Task<UnlockChapterResponse> UnlockChapter(UnlockChapterRequest request, ServerCallContext context)
        {
            if (!int.TryParse(request.UserId, out var userId) || !Guid.TryParse(request.ChapterId, out var chapterId))
            {
                return new UnlockChapterResponse { Success = false, AlreadyPurchased = false };
            }

            var result = await _chapterService.UnlockChapterAsync(userId, chapterId);
            return new UnlockChapterResponse { Success = result.Success, AlreadyPurchased = result.AlreadyPurchased };
        }

        public override async Task<CheckResponse> IsChapterPurchased(CheckRequest request, ServerCallContext context)
        {
            if (!int.TryParse(request.UserId, out var userId) || !Guid.TryParse(request.ChapterId, out var chapterId))
            {
                return new CheckResponse { IsPurchased = false };
            }

            var hasPurchased = await _chapterService.IsChapterPurchasedAsync(userId, chapterId);
            return new CheckResponse { IsPurchased = hasPurchased };
        }

        public override async Task<GetChapterInfoResponse> GetChapterInfo(GetChapterInfoRequest request, ServerCallContext context)
        {
            if (!Guid.TryParse(request.ChapterId, out var chapterId))
            {
                return new GetChapterInfoResponse { Exists = false };
            }

            var chapter = await _chapterService.GetChapterInfoAsync(chapterId);
            if (chapter == null)
            {
                return new GetChapterInfoResponse { Exists = false };
            }

            return new GetChapterInfoResponse
            {
                Exists = true,
                ComicId = chapter.ComicId.ToString(),
                UnitPrice = (double)chapter.UnitPrice,
                Title = chapter.Title,
                ChapterNumber = chapter.ChapterNumber,
                Slug = chapter.Slug,
                Status = chapter.Status
            };
        }

        public override async Task<GetChapterCountResponse> GetChapterCount(
            GetChapterCountRequest request,
            ServerCallContext context)
        {
            if (!Guid.TryParse(request.ComicId, out var comicId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "ComicId is invalid."));
            }

            var count = await _chapterService.GetChapterCountAsync(comicId);
            return new GetChapterCountResponse { Count = count };
        }

        public override async Task<GetPurchasedComicIdsResponse> GetPurchasedComicIds(
            GetPurchasedComicIdsRequest request,
            ServerCallContext context)
        {
            if (request.UserId <= 0)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "UserId is invalid."));
            }

            var comicIds = await _chapterService.GetPurchasedComicIdsAsync(request.UserId);
            var response = new GetPurchasedComicIdsResponse();
            response.ComicIds.AddRange(comicIds.Select(x => x.ToString()));
            return response;
        }

        public override async Task<GetUserPurchaseActivitiesResponse> GetUserPurchaseActivities(
            GetUserPurchaseActivitiesRequest request,
            ServerCallContext context)
        {
            if (request.UserId <= 0)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "UserId is invalid."));
            }

            var purchases = await _chapterService.GetUserPurchaseActivitiesAsync(request.UserId);
            var response = new GetUserPurchaseActivitiesResponse();
            response.Activities.AddRange(purchases.Select(purchase => new PurchaseActivity
            {
                ChapterId = purchase.ChapterId.ToString(),
                PurchasedAtUnixSeconds = new DateTimeOffset(purchase.PurchasedAt.ToUniversalTime()).ToUnixTimeSeconds()
            }));
            return response;
        }
    }
}
