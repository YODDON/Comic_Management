using System.Collections.Generic;
using System.Threading.Tasks;
using ComicAPI.Entities;

namespace ComicAPI.Interfaces
{
    public interface ICategoryRepository
    {
        Task<(List<Category> Items, int TotalCount)> GetCategoriesAsync(int pageNumber, int pageSize, string? searchTerm);
        Task<Category?> GetCategoryByIdAsync(System.Guid id);
        Task<Category?> GetCategoryByNameAsync(string name);
        Task AddCategoryAsync(Category category);
        Task UpdateCategoryAsync(Category category);
        Task DeleteCategoryAsync(Category category);
        Task<bool> IsCategoryUsedAsync(System.Guid categoryId);
        Task<int> SaveChangesAsync();
    }
}
