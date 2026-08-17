using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using ComicAPI.Application.DTOs;
using ComicAPI.Domain.Interfaces;
using MediatR;
using SharedKernel.Responses;

namespace ComicAPI.Application.Features.Categories.Queries
{
    public class GetCategoryByIdQuery : IRequest<ApiResponse<CategoryDto>>
    {
        public Guid Id { get; set; }
    }

    public class GetCategoryByIdQueryHandler : IRequestHandler<GetCategoryByIdQuery, ApiResponse<CategoryDto>>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IMapper _mapper;

        public GetCategoryByIdQueryHandler(ICategoryRepository categoryRepository, IMapper mapper)
        {
            _categoryRepository = categoryRepository;
            _mapper = mapper;
        }

        public async Task<ApiResponse<CategoryDto>> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
        {
            var category = await _categoryRepository.GetCategoryByIdAsync(request.Id);
            if (category == null)
            {
                return new ApiResponse<CategoryDto>(null, "Category not found", 404);
            }

            return new ApiResponse<CategoryDto>(_mapper.Map<CategoryDto>(category), "Category retrieved successfully.", 200);
        }
    }
}
