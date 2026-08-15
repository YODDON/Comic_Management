using Microsoft.EntityFrameworkCore;
using SocialAPI.Data;
using SocialAPI.Entities;
using SocialAPI.Interfaces;

namespace SocialAPI.Repositories;

public class ReadingHistoryRepository : IReadingHistoryRepository
{
    private readonly SocialDbContext _context;

    public ReadingHistoryRepository(SocialDbContext context) => _context = context;

    public async Task<(List<ReadingHistory> Items, int TotalCount)> GetByUserAsync(
        Guid userId, int page, int pageSize)
    {
        var query = _context.ReadingHistories.AsNoTracking().Where(item => item.UserId == userId);
        var totalCount = await query.CountAsync();
        var items = await query.OrderByDescending(item => item.ReadAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, totalCount);
    }

    public Task<ReadingHistory?> GetForUpdateAsync(Guid userId, Guid comicId) =>
        _context.ReadingHistories.FirstOrDefaultAsync(history =>
            history.UserId == userId && history.ComicId == comicId);

    public void Add(ReadingHistory history) => _context.ReadingHistories.Add(history);
    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
