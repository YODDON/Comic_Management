using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using ComicAPI.Application.DTOs;
using ComicAPI.Application.Features.Comics.Helpers;
using ComicAPI.Domain.Interfaces;
using MediatR;
using SharedKernel.Enums;
using SharedKernel.Responses;
using UserAPI.Protos;

namespace ComicAPI.Application.Features.Comics.Queries
{
    public class GetComicsQuery : IRequest<ApiResponse<PagedResult<ComicSummaryDto>>>
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string? Search { get; set; }
        public List<Guid>? CategoryIds { get; set; }
        public ComicStatus? Status { get; set; }
    }

    public class GetComicsQueryHandler : IRequestHandler<GetComicsQuery, ApiResponse<PagedResult<ComicSummaryDto>>>
    {
        private readonly IComicRepository _comicRepository;
        private readonly IMapper _mapper;
        private readonly UserService.UserServiceClient _userServiceClient;

        public GetComicsQueryHandler(IComicRepository comicRepository, IMapper mapper, UserService.UserServiceClient userServiceClient)
        {
            _comicRepository = comicRepository;
            _mapper = mapper;
            _userServiceClient = userServiceClient;
        }

        public async Task<ApiResponse<PagedResult<ComicSummaryDto>>> Handle(GetComicsQuery request, CancellationToken cancellationToken)
        {
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 ? 10 : (request.PageSize > 100 ? 100 : request.PageSize);

            var (items, totalCount) = await _comicRepository.GetComicsAsync(pageNumber, pageSize, request.Search, request.CategoryIds, request.Status);

            var dtos = _mapper.Map<List<ComicSummaryDto>>(items);
            await ComicHelper.EnrichComicsWithAuthorNamesAsync(_userServiceClient, dtos, items);
            await ComicHelper.EnrichOutstandingFlagsAsync(_comicRepository, dtos);

            var pagedResult = new PagedResult<ComicSummaryDto>(dtos, totalCount, pageNumber, pageSize);

            return new ApiResponse<PagedResult<ComicSummaryDto>>(pagedResult, "Comics retrieved successfully.", 200);
        }
    }
}
