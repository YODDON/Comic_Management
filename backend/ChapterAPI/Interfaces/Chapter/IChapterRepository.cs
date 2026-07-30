using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChapterAPI.Entities;

namespace ChapterAPI.Interfaces
{
    public interface IChapterRepository
    {
        Task<(List<Chapter> Items, int TotalCount)> GetChaptersAsync(Guid? comicId, string? search, string? status, int pageNumber, int pageSize);
        Task<Chapter?> GetChapterByIdAsync(Guid id, bool includePages);
        Task<bool> HasUserPurchasedChapterAsync(int userId, Guid chapterId);
        Task<HashSet<Guid>> GetPurchasedChapterIdsAsync(int userId, IEnumerable<Guid> chapterIds);
        Task<Chapter> AddChapterAsync(Chapter chapter);
        Task<Chapter> UpdateChapterAsync(Chapter chapter);
        Task<bool> DeleteChapterAsync(Chapter chapter);
        Task<bool> IsSlugUniqueAsync(Guid comicId, string slug);
        Task<Chapter?> GetChapterBySlugAsync(Guid comicId, string slug);
        Task<int> GetMaxPageNumberAsync(Guid chapterId);
        Task AddChapterPagesAsync(List<ChapterPage> pages);
        Task UpdateChapterPagesAsync(List<ChapterPage> pages);
        Task DeleteChapterPageAsync(ChapterPage page);
        Task DeleteChapterPagesAsync(Chapter chapter, List<ChapterPage> pages);
        Task<bool> UnlockChapterAsync(int userId, Guid chapterId);
        Task<int> GetChapterCountAsync(Guid comicId);
        Task<List<Guid>> GetPurchasedComicIdsAsync(int userId);
        Task<List<UserPurchase>> GetUserPurchaseActivitiesAsync(int userId);
    }
}
