using System;
using System.Threading.Tasks;
using ComicAPI.Interfaces;
using ComicAPI.Protos;
using Grpc.Core;

namespace ComicAPI.GrpcServices
{
    public class ComicGrpcService : ComicGrpc.ComicGrpcBase
    {
        private readonly IComicService _comicService;

        public ComicGrpcService(IComicService comicService)
        {
            _comicService = comicService;
        }

        public override async Task<CheckComicExistsResponse> CheckComicExists(CheckComicExistsRequest request, ServerCallContext context)
        {
            if (Guid.TryParse(request.ComicId, out var comicId))
            {
                var comic = await _comicService.CheckComicExistsAsync(comicId);
                return new CheckComicExistsResponse
                {
                    Exists = comic.Exists,
                    Status = comic.Status
                };
            }
            return new CheckComicExistsResponse { Exists = false };
        }

        public override async Task<IncrementComicViewResponse> IncrementComicView(IncrementComicViewRequest request, ServerCallContext context)
        {
            if (!Guid.TryParse(request.ComicId, out var comicId))
            {
                return new IncrementComicViewResponse { Success = false };
            }

            var success = await _comicService.IncrementViewCountAsync(comicId);
            return new IncrementComicViewResponse { Success = success };
        }
    }
}
