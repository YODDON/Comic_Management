using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using ComicAPI.Application.DTOs;
using ComicAPI.Application.Features.Comics.Helpers;
using ComicAPI.Domain.Interfaces;
using MediatR;
using SharedKernel.Responses;
using UserAPI.Protos;

namespace ComicAPI.Application.Features.Comics.Queries
{
    public class GetOutstandingComicsPagedQuery : IRequest<ApiResponse<PagedResult<ComicSummaryDto>>>
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }

    public class GetOutstandingComicsPagedQueryHandler : IRequestHandler<GetOutstandingComicsPagedQuery, ApiResponse<PagedResult<ComicSummaryDto>>>
    {
        private readonly IComicRepository _comicRepository;
        private readonly IMapper _mapper;
        private readonly UserService.UserServiceClient _userServiceClient;

        public GetOutstandingComicsPagedQueryHandler(IComicRepository comicRepository, IMapper mapper, UserService.UserServiceClient userServiceClient)
        {
            _comicRepository = comicRepository;
            _mapper = mapper;
            _userServiceClient = userServiceClient;
        }

        public async Task<ApiResponse<PagedResult<ComicSummaryDto>>> Handle(GetOutstandingComicsPagedQuery request, CancellationToken cancellationToken)
        {
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 ? 20 : (request.PageSize > 100 ? 100 : request.PageSize);

            var (comics, totalCount) = await _comicRepository.GetOutstandingComicsAsync(pageNumber, pageSize);
            
            var dtos = _mapper.Map<List<ComicSummaryDto>>(comics);
            await ComicHelper.EnrichComicsWithAuthorNamesAsync(_userServiceClient, dtos, comics);

            var result = new PagedResult<ComicSummaryDto>(dtos, totalCount, pageNumber, pageSize);
            
            return new ApiResponse<PagedResult<ComicSummaryDto>>(result, "Outstanding comics retrieved.", 200);
        }
    }
}
