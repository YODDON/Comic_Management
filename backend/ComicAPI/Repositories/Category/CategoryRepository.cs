using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ComicAPI.Data;
using ComicAPI.Entities;
using ComicAPI.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ComicAPI.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly ComicDbContext _context;

        public CategoryRepository(ComicDbContext context)
        {
            _context = context;
        }

        public async Task<(List<Category> Items, int TotalCount)> GetCategoriesAsync(int pageNumber, int pageSize, string? searchTerm)
        {
            var query = _context.Categories.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lowerSearch = searchTerm.ToLower();
                query = query.Where(c => c.Name.ToLower().Contains(lowerSearch) || c.Slug.ToLower().Contains(lowerSearch));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(c => c.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<Category?> GetCategoryByIdAsync(System.Guid id)
        {
            return await _context.Categories.FindAsync(id);
        }

        public async Task<Category?> GetCategoryByNameAsync(string name)
        {
            return await _context.Categories.FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());
        }

        public async Task AddCategoryAsync(Category category)
        {
            await _context.Categories.AddAsync(category);
        }

        public Task UpdateCategoryAsync(Category category)
        {
            _context.Categories.Update(category);
            return Task.CompletedTask;
        }

        public Task DeleteCategoryAsync(Category category)
        {
            _context.Categories.Remove(category);
            return Task.CompletedTask;
        }

        public async Task<bool> IsCategoryUsedAsync(System.Guid categoryId)
        {
            return await _context.ComicCategories.AnyAsync(cc => cc.CategoryId == categoryId);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
