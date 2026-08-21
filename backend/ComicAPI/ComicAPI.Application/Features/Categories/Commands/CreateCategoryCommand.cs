using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using ComicAPI.Application.DTOs;
using ComicAPI.Domain.Entities;
using ComicAPI.Domain.Interfaces;
using MediatR;
using SharedKernel.Responses;

namespace ComicAPI.Application.Features.Categories.Commands
{
    public class CreateCategoryCommand : IRequest<ApiResponse<CategoryDto>>
    {
        public CreateCategoryRequestDto Request { get; set; } = null!;
    }

    public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, ApiResponse<CategoryDto>>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IMapper _mapper;

        public CreateCategoryCommandHandler(ICategoryRepository categoryRepository, IMapper mapper)
        {
            _categoryRepository = categoryRepository;
            _mapper = mapper;
        }

        public async Task<ApiResponse<CategoryDto>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
        {
            var existing = await _categoryRepository.GetCategoryByNameAsync(request.Request.Name);
            if (existing != null)
            {
                return new ApiResponse<CategoryDto>(null, "Category name already exists.", 409);
            }

            var category = new Category
            {
                Name = request.Request.Name,
                Slug = SharedKernel.Utilities.SlugGenerator.GenerateSlug(request.Request.Name),
                Tag = request.Request.Tag
            };

            await _categoryRepository.AddCategoryAsync(category);
            await _categoryRepository.SaveChangesAsync();

            return new ApiResponse<CategoryDto>(_mapper.Map<CategoryDto>(category), "Category created successfully.", 201);
        }
    }
}
