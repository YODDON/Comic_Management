using System;
using System.Threading.Tasks;
using ComicAPI.Interfaces;
using ComicAPI.Protos;
using Grpc.Core;

namespace ComicAPI.Services
{
    public class ComicGrpcService : ComicGrpc.ComicGrpcBase
    {
        private readonly IComicRepository _comicRepository;

        public ComicGrpcService(IComicRepository comicRepository)
        {
            _comicRepository = comicRepository;
        }

        public override async Task<CheckComicExistsResponse> CheckComicExists(CheckComicExistsRequest request, ServerCallContext context)
        {
            if (Guid.TryParse(request.ComicId, out var comicId))
            {
                var comic = await _comicRepository.GetComicByIdAsync(comicId);
                return new CheckComicExistsResponse
                {
                    Exists = comic != null,
                    Status = comic?.Status.ToString() ?? string.Empty
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

            var success = await _comicRepository.IncrementViewCountAsync(comicId);
            return new IncrementComicViewResponse { Success = success };
        }
    }
}
