using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using ComicAPI.Application.DTOs;
using ComicAPI.Domain.Entities;
using ComicAPI.Domain.Interfaces;
using ComicAPI.Application.Interfaces;
using SharedKernel.Responses;

namespace ComicAPI.Application.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IMapper _mapper;

        public CategoryService(ICategoryRepository categoryRepository, IMapper mapper)
        {
            _categoryRepository = categoryRepository;
            _mapper = mapper;
        }

        public async Task<ApiResponse<PagedResult<CategoryDto>>> GetCategoriesAsync(int pageNumber, int pageSize, string? searchTerm)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var (items, totalCount) = await _categoryRepository.GetCategoriesAsync(pageNumber, pageSize, searchTerm);

            var dtos = _mapper.Map<List<CategoryDto>>(items);

            var pagedResult = new PagedResult<CategoryDto>(dtos, totalCount, pageNumber, pageSize);

            return new ApiResponse<PagedResult<CategoryDto>>(pagedResult, "Categories retrieved successfully.", 200);
        }

        public async Task<ApiResponse<CategoryDto>> GetCategoryByIdAsync(System.Guid id)
        {
            var category = await _categoryRepository.GetCategoryByIdAsync(id);
            if (category == null)
            {
                return new ApiResponse<CategoryDto>(null, "Category not found", 404);
            }

            return new ApiResponse<CategoryDto>(_mapper.Map<CategoryDto>(category), "Category retrieved successfully.", 200);
        }

        public async Task<ApiResponse<CategoryDto>> CreateCategoryAsync(CreateCategoryRequestDto request)
        {
            var existing = await _categoryRepository.GetCategoryByNameAsync(request.Name);
            if (existing != null)
            {
                return new ApiResponse<CategoryDto>(null, "Category name already exists.", 409);
            }

            var category = new Category
            {
                Name = request.Name,
                Slug = SharedKernel.Utilities.SlugGenerator.GenerateSlug(request.Name),
                Tag = request.Tag
            };

            await _categoryRepository.AddCategoryAsync(category);
            await _categoryRepository.SaveChangesAsync();

            return new ApiResponse<CategoryDto>(_mapper.Map<CategoryDto>(category), "Category created successfully.", 201);
        }

        public async Task<ApiResponse<CategoryDto>> UpdateCategoryAsync(System.Guid id, UpdateCategoryRequestDto request)
        {
            var category = await _categoryRepository.GetCategoryByIdAsync(id);
            if (category == null)
            {
                return new ApiResponse<CategoryDto>(null, "Category not found", 404);
            }

            var existing = await _categoryRepository.GetCategoryByNameAsync(request.Name);
            if (existing != null && existing.Id != id)
            {
                return new ApiResponse<CategoryDto>(null, "Category name already exists.", 409);
            }

            category.Name = request.Name;
            category.Slug = SharedKernel.Utilities.SlugGenerator.GenerateSlug(request.Name);
            category.Tag = request.Tag;

            await _categoryRepository.UpdateCategoryAsync(category);
            await _categoryRepository.SaveChangesAsync();

            return new ApiResponse<CategoryDto>(_mapper.Map<CategoryDto>(category), "Category updated successfully.", 200);
        }

        public async Task<ApiResponse<bool>> DeleteCategoryAsync(System.Guid id)
        {
            var category = await _categoryRepository.GetCategoryByIdAsync(id);
            if (category == null)
            {
                return new ApiResponse<bool>(false, "Category not found", 404);
            }

            var isUsed = await _categoryRepository.IsCategoryUsedAsync(id);
            if (isUsed)
            {
                return new ApiResponse<bool>(false, "Category is currently in use by one or more comics.", 400);
            }

            await _categoryRepository.DeleteCategoryAsync(category);
            await _categoryRepository.SaveChangesAsync();

            return new ApiResponse<bool>(true, "Category deleted successfully.", 200);
        }
    }
}
