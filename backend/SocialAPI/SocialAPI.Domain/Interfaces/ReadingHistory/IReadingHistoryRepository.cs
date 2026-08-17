using SocialAPI.Entities;

namespace SocialAPI.Interfaces;

public interface IReadingHistoryRepository
{
    Task<(List<ReadingHistory> Items, int TotalCount)> GetByUserAsync(Guid userId, int page, int pageSize);
    Task<ReadingHistory?> GetForUpdateAsync(Guid userId, Guid comicId);
    void Add(ReadingHistory history);
    Task SaveChangesAsync();
}
