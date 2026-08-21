using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using ChapterAPI.Protos;
using ComicAPI.Application.DTOs;
using ComicAPI.Domain.Interfaces;
using MediatR;
using SharedKernel.Responses;
using UserAPI.Protos;

namespace ComicAPI.Application.Features.Comics.Queries
{
    public class GetComicBySlugQuery : IRequest<ApiResponse<ComicDetailDto>>
    {
        public string Slug { get; set; } = null!;
    }

    public class GetComicBySlugQueryHandler : IRequestHandler<GetComicBySlugQuery, ApiResponse<ComicDetailDto>>
    {
        private readonly IComicRepository _comicRepository;
        private readonly IMapper _mapper;
        private readonly UserService.UserServiceClient _userServiceClient;
        private readonly ChapterGrpc.ChapterGrpcClient _chapterServiceClient;

        public GetComicBySlugQueryHandler(IComicRepository comicRepository, IMapper mapper, UserService.UserServiceClient userServiceClient, ChapterGrpc.ChapterGrpcClient chapterServiceClient)
        {
            _comicRepository = comicRepository;
            _mapper = mapper;
            _userServiceClient = userServiceClient;
            _chapterServiceClient = chapterServiceClient;
        }

        public async Task<ApiResponse<ComicDetailDto>> Handle(GetComicBySlugQuery request, CancellationToken cancellationToken)
        {
            var comic = await _comicRepository.GetComicBySlugAsync(request.Slug);
            if (comic == null)
            {
                return new ApiResponse<ComicDetailDto>(null, "Comic not found.", 404);
            }

            var dto = _mapper.Map<ComicDetailDto>(comic);
            dto.IsOutstanding = await _comicRepository.IsComicOutstandingAsync(comic.Id);

            if (!string.IsNullOrWhiteSpace(comic.Author))
            {
                dto.AuthorName = comic.Author.Trim();
            }
            else
            {
                try
                {
                    var userRequest = new GetUsersByIdsRequest();
                    userRequest.Ids.Add(comic.OwnerId.ToString());

                    var response = await _userServiceClient.GetUsersByIdsAsync(userRequest, cancellationToken: cancellationToken);
                    var user = response.Users.FirstOrDefault();
                    dto.AuthorName = user != null ? user.Username : "Unknown";
                }
                catch
                {
                    dto.AuthorName = "Unknown";
                }
            }

            try
            {
                var chapterResponse = await _chapterServiceClient.GetChapterCountAsync(
                    new GetChapterCountRequest { ComicId = comic.Id.ToString() }, cancellationToken: cancellationToken);
                dto.ChapterCount = chapterResponse.Count;
            }
            catch
            {
                dto.ChapterCount = 0;
            }

            return new ApiResponse<ComicDetailDto>(dto, "Comic retrieved successfully.", 200);
        }
    }
}
