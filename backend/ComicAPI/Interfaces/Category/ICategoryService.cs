using System.Threading.Tasks;
using ComicAPI.DTOs;
using SharedKernel.Responses;

namespace ComicAPI.Interfaces
{
    public interface ICategoryService
    {
        Task<ApiResponse<PagedResult<CategoryDto>>> GetCategoriesAsync(int pageNumber, int pageSize, string? searchTerm);
        Task<ApiResponse<CategoryDto>> GetCategoryByIdAsync(System.Guid id);
        Task<ApiResponse<CategoryDto>> CreateCategoryAsync(CreateCategoryRequestDto request);
        Task<ApiResponse<CategoryDto>> UpdateCategoryAsync(System.Guid id, UpdateCategoryRequestDto request);
        Task<ApiResponse<bool>> DeleteCategoryAsync(System.Guid id);
    }
}
