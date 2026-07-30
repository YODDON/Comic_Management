using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ChapterAPI.Data;
using ChapterAPI.Entities;
using ChapterAPI.Interfaces;

namespace ChapterAPI.Repositories
{
    public class ChapterRepository : IChapterRepository
    {
        private readonly ChapterDbContext _context;

        public ChapterRepository(ChapterDbContext context)
        {
            _context = context;
        }

        public async Task<(List<Chapter> Items, int TotalCount)> GetChaptersAsync(Guid? comicId, string? search, string? status, int pageNumber, int pageSize)
        {
            var query = _context.Chapters.AsQueryable();

            if (comicId.HasValue)
            {
                query = query.Where(c => c.ComicId == comicId.Value);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => c.Title.Contains(search));
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(c => c.Status == status);
            }

            var totalCount = await query.CountAsync();
            
            var items = await query
                .OrderBy(c => c.ChapterNumber)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<Chapter?> GetChapterByIdAsync(Guid id, bool includePages)
        {
            var query = _context.Chapters.AsQueryable();

            if (includePages)
            {
                query = query.Include(c => c.ChapterPages.OrderBy(p => p.PageNumber));
            }

            return await query.FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<bool> HasUserPurchasedChapterAsync(int userId, Guid chapterId)
        {
            return await _context.UserPurchases
                .AnyAsync(up => up.UserId == userId && up.ChapterId == chapterId);
        }

        public async Task<HashSet<Guid>> GetPurchasedChapterIdsAsync(
            int userId,
            IEnumerable<Guid> chapterIds)
        {
            var ids = chapterIds.Distinct().ToList();
            if (ids.Count == 0) return new HashSet<Guid>();

            var purchasedIds = await _context.UserPurchases
                .AsNoTracking()
                .Where(purchase => purchase.UserId == userId && ids.Contains(purchase.ChapterId))
                .Select(purchase => purchase.ChapterId)
                .ToListAsync();
            return purchasedIds.ToHashSet();
        }

        public async Task<Chapter> AddChapterAsync(Chapter chapter)
        {
            _context.Chapters.Add(chapter);
            await _context.SaveChangesAsync();
            return chapter;
        }

        public async Task<Chapter> UpdateChapterAsync(Chapter chapter)
        {
            _context.Chapters.Update(chapter);
            await _context.SaveChangesAsync();
            return chapter;
        }

        public async Task<bool> DeleteChapterAsync(Chapter chapter)
        {
            _context.Chapters.Remove(chapter);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsSlugUniqueAsync(Guid comicId, string slug)
        {
            return !await _context.Chapters.AnyAsync(c => c.ComicId == comicId && c.Slug == slug);
        }

        public async Task<Chapter?> GetChapterBySlugAsync(Guid comicId, string slug)
        {
            return await _context.Chapters.FirstOrDefaultAsync(c => c.ComicId == comicId && c.Slug == slug);
        }

        public async Task<int> GetMaxPageNumberAsync(Guid chapterId)
        {
            var maxPage = await _context.ChapterPages
                .Where(p => p.ChapterId == chapterId)
                .OrderByDescending(p => p.PageNumber)
                .Select(p => p.PageNumber)
                .FirstOrDefaultAsync();
            return maxPage;
        }

        public async Task AddChapterPagesAsync(List<ChapterPage> pages)
        {
            if (pages.Any())
            {
                var chapterId = pages.First().ChapterId;
                _context.ChapterPages.AddRange(pages);
                
                var chapter = await _context.Chapters.FindAsync(chapterId);
                if (chapter != null)
                {
                    chapter.PageCount += pages.Count;
                    _context.Chapters.Update(chapter);
                }
                
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateChapterPagesAsync(List<ChapterPage> pages)
        {
            if (pages.Any())
            {
                _context.ChapterPages.UpdateRange(pages);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteChapterPageAsync(ChapterPage page)
        {
            _context.ChapterPages.Remove(page);
            
            var chapter = await _context.Chapters.FindAsync(page.ChapterId);
            if (chapter != null)
            {
                var remainingPages = await _context.ChapterPages
                    .Where(item => item.ChapterId == page.ChapterId && item.Id != page.Id)
                    .OrderBy(item => item.PageNumber)
                    .ToListAsync();
                for (var index = 0; index < remainingPages.Count; index++)
                {
                    remainingPages[index].PageNumber = index + 1;
                }
                chapter.PageCount = remainingPages.Count;
                chapter.UpdatedAt = DateTime.UtcNow;
            }
            
            await _context.SaveChangesAsync();
        }

        public async Task DeleteChapterPagesAsync(Chapter chapter, List<ChapterPage> pages)
        {
            var deletedIds = pages.Select(page => page.Id).ToHashSet();
            _context.ChapterPages.RemoveRange(pages);

            var remainingPages = chapter.ChapterPages
                .Where(page => !deletedIds.Contains(page.Id))
                .OrderBy(page => page.PageNumber)
                .ToList();
            for (var index = 0; index < remainingPages.Count; index++)
            {
                remainingPages[index].PageNumber = index + 1;
            }

            chapter.PageCount = remainingPages.Count;
            chapter.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task<bool> UnlockChapterAsync(int userId, Guid chapterId)
        {
            var chapter = await _context.Chapters.FindAsync(chapterId);
            if (chapter == null) return false;

            var existingPurchase = await _context.UserPurchases
                .FirstOrDefaultAsync(p => p.UserId == userId && p.ChapterId == chapterId);
            
            if (existingPurchase != null) return true;

            var purchase = new ChapterAPI.Entities.UserPurchase
            {
                UserId = userId,
                ChapterId = chapterId,
                PurchasedAt = DateTime.UtcNow
            };

            _context.UserPurchases.Add(purchase);
            await _context.SaveChangesAsync();
            return true;
        }

        public Task<int> GetChapterCountAsync(Guid comicId)
        {
            return _context.Chapters.CountAsync(c =>
                c.ComicId == comicId && c.Status == "Published");
        }

        public Task<List<Guid>> GetPurchasedComicIdsAsync(int userId)
        {
            return _context.UserPurchases
                .Where(x => x.UserId == userId)
                .Join(_context.Chapters,
                    purchase => purchase.ChapterId,
                    chapter => chapter.Id,
                    (_, chapter) => chapter.ComicId)
                .Distinct()
                .ToListAsync();
        }

        public Task<List<UserPurchase>> GetUserPurchaseActivitiesAsync(int userId)
        {
            return _context.UserPurchases
                .AsNoTracking()
                .Where(purchase => purchase.UserId == userId)
                .OrderBy(purchase => purchase.PurchasedAt)
                .ToListAsync();
        }
    }
}
