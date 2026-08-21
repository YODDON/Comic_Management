using System.Linq;
using System.Threading.Tasks;
using ComicAPI.Infrastructure.Data;
using ComicAPI.Domain.Entities;
using ComicAPI.Domain.Interfaces;
using ComicAPI.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Enums;

namespace ComicAPI.Infrastructure.Repositories
{
    public class ComicRepository : IComicRepository
    {
        private readonly ComicDbContext _context;

        public ComicRepository(ComicDbContext context)
        {
            _context = context;
        }

        public async Task<(List<Comic> Items, int TotalCount)> GetComicsAsync(int pageNumber, int pageSize, string? search, List<Guid>? categoryIds, ComicStatus? status)
        {
            var query = _context.Comics
                .Include(c => c.ComicCategories)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(c => c.Title.ToLower().Contains(lowerSearch) || c.Description.ToLower().Contains(lowerSearch));
            }

            if (categoryIds != null && categoryIds.Any())
            {
                query = query.Where(c => c.ComicCategories.Any(cc => categoryIds.Contains(cc.CategoryId)));
            }

            if (status.HasValue)
            {
                query = query.Where(c => c.Status == status.Value);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(c => c.UpdatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<List<Comic>> GetHotComicsAsync(int limit)
        {
            return await _context.Comics
                .Where(c => c.Status != SharedKernel.Enums.ComicStatus.Dropped)
                .OrderByDescending(c => c.ViewCount)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<Comic>> GetOutstandingComicsAsync(int limit)
        {
            var now = System.DateTime.UtcNow;
            return await _context.Outstandings
                .Include(o => o.Comic)
                .Where(o =>
                    o.Comic.Status == ComicStatus.Ongoing &&
                    o.StartDate <= now &&
                    o.EndDate >= now)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => o.Comic)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<(List<Comic> Items, int TotalCount)> GetOutstandingComicsAsync(
            int pageNumber,
            int pageSize)
        {
            var now = System.DateTime.UtcNow;
            var query = _context.Outstandings
                .AsNoTracking()
                .Where(o =>
                    o.Comic.Status == ComicStatus.Ongoing &&
                    o.StartDate <= now &&
                    o.EndDate >= now);

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(o => o.Comic)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<List<Comic>> GetLastCompletedComicsAsync(int limit)
        {
            return await _context.Comics
                .Where(c => c.Status == ComicStatus.Completed)
                .OrderByDescending(c => c.UpdatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<Comic?> GetComicBySlugAsync(string slug)
        {
            return await _context.Comics
                .Include(c => c.ComicCategories)
                    .ThenInclude(cc => cc.Category)
                .FirstOrDefaultAsync(c => c.Slug == slug);
        }

        public Task<bool> SlugExistsAsync(string slug, Guid? excludedComicId = null)
        {
            return _context.Comics.AnyAsync(comic =>
                comic.Slug == slug &&
                (!excludedComicId.HasValue || comic.Id != excludedComicId.Value));
        }

        public async Task<Comic?> GetComicByIdAsync(System.Guid id)
        {
            return await _context.Comics
                .Include(c => c.ComicCategories)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<(List<Comic> Items, int TotalCount)> GetComicsByOwnerAsync(int ownerId, int pageNumber, int pageSize)
        {
            var query = _context.Comics.Where(c => c.OwnerId == ownerId).AsQueryable();

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(List<Comic> Items, int TotalCount)> GetComicsByIdsAsync(List<System.Guid> ids, int pageNumber, int pageSize)
        {
            var query = _context.Comics.Where(c => ids.Contains(c.Id)).AsQueryable();

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task AddComicAsync(Comic comic)
        {
            await _context.Comics.AddAsync(comic);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateComicAsync(
            Comic comic,
            IReadOnlyCollection<Guid>? categoryIds = null)
        {
            comic.UpdatedAt = System.DateTime.UtcNow;

            if (categoryIds is null)
            {
                await _context.SaveChangesAsync();
                return;
            }

            // Relationship entities previously relied on collection change detection. A new
            // ComicCategory has a client-generated Guid key, so EF can classify it as Modified
            // instead of Added and issue an UPDATE that affects zero rows. Replace the join rows
            // explicitly so every selected category is always inserted with the correct state.
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var trackedLink in comic.ComicCategories.ToList())
                {
                    _context.Entry(trackedLink).State = EntityState.Detached;
                }

                await _context.ComicCategories
                    .Where(link => link.ComicId == comic.Id)
                    .ExecuteDeleteAsync();

                var newLinks = categoryIds
                    .Distinct()
                    .Select(categoryId => new ComicCategory
                    {
                        ComicId = comic.Id,
                        CategoryId = categoryId
                    })
                    .ToList();

                comic.ComicCategories = newLinks;
                await _context.ComicCategories.AddRangeAsync(newLinks);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> IncrementViewCountAsync(System.Guid comicId)
        {
            var affected = await _context.Comics
                .Where(c => c.Id == comicId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(c => c.ViewCount, c => c.ViewCount + 1));
            return affected > 0;
        }

        public async Task<bool> IsComicOutstandingAsync(System.Guid comicId)
        {
            var now = System.DateTime.UtcNow;
            return await _context.Outstandings
                .AnyAsync(o => o.ComicId == comicId && o.StartDate <= now && o.EndDate >= now);
        }

        public async Task AddOutstandingAsync(Outstanding outstanding)
        {
            await _context.Outstandings.AddAsync(outstanding);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveOutstandingAsync(System.Guid comicId)
        {
            var entries = await _context.Outstandings
                .Where(o => o.ComicId == comicId)
                .ToListAsync();
            if (!entries.Any()) return;

            _context.Outstandings.RemoveRange(entries);
            await _context.SaveChangesAsync();
        }
    }
}
