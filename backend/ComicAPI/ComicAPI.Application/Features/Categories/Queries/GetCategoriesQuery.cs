using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using ComicAPI.Application.DTOs;
using ComicAPI.Domain.Interfaces;
using MediatR;
using SharedKernel.Responses;

namespace ComicAPI.Application.Features.Categories.Queries
{
    public class GetCategoriesQuery : IRequest<ApiResponse<PagedResult<CategoryDto>>>
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string? SearchTerm { get; set; }
    }

    public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, ApiResponse<PagedResult<CategoryDto>>>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IMapper _mapper;

        public GetCategoriesQueryHandler(ICategoryRepository categoryRepository, IMapper mapper)
        {
            _categoryRepository = categoryRepository;
            _mapper = mapper;
        }

        public async Task<ApiResponse<PagedResult<CategoryDto>>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
        {
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 ? 10 : (request.PageSize > 100 ? 100 : request.PageSize);

            var (items, totalCount) = await _categoryRepository.GetCategoriesAsync(pageNumber, pageSize, request.SearchTerm);

            var dtos = _mapper.Map<List<CategoryDto>>(items);
            var pagedResult = new PagedResult<CategoryDto>(dtos, totalCount, pageNumber, pageSize);

            return new ApiResponse<PagedResult<CategoryDto>>(pagedResult, "Categories retrieved successfully.", 200);
        }
    }
}
