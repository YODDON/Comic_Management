using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using ComicAPI.Application.DTOs;
using ComicAPI.Domain.Interfaces;
using MediatR;
using SharedKernel.Responses;

namespace ComicAPI.Application.Features.Categories.Commands
{
    public class UpdateCategoryCommand : IRequest<ApiResponse<CategoryDto>>
    {
        public Guid Id { get; set; }
        public UpdateCategoryRequestDto Request { get; set; } = null!;
    }

    public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, ApiResponse<CategoryDto>>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IMapper _mapper;

        public UpdateCategoryCommandHandler(ICategoryRepository categoryRepository, IMapper mapper)
        {
            _categoryRepository = categoryRepository;
            _mapper = mapper;
        }

        public async Task<ApiResponse<CategoryDto>> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = await _categoryRepository.GetCategoryByIdAsync(request.Id);
            if (category == null)
            {
                return new ApiResponse<CategoryDto>(null, "Category not found", 404);
            }

            var existing = await _categoryRepository.GetCategoryByNameAsync(request.Request.Name);
            if (existing != null && existing.Id != request.Id)
            {
                return new ApiResponse<CategoryDto>(null, "Category name already exists.", 409);
            }

            category.Name = request.Request.Name;
            category.Slug = SharedKernel.Utilities.SlugGenerator.GenerateSlug(request.Request.Name);
            category.Tag = request.Request.Tag;

            await _categoryRepository.UpdateCategoryAsync(category);
            await _categoryRepository.SaveChangesAsync();

            return new ApiResponse<CategoryDto>(_mapper.Map<CategoryDto>(category), "Category updated successfully.", 200);
        }
    }
}
