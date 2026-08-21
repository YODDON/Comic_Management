using Microsoft.EntityFrameworkCore;
using SocialAPI.Data;
using SocialAPI.DTOs;
using SocialAPI.Interfaces;

namespace SocialAPI.Repositories;

public class SocialActivityRepository : ISocialActivityRepository
{
    private readonly SocialDbContext _context;

    public SocialActivityRepository(SocialDbContext context) => _context = context;

    public async Task<SocialActivitySnapshotDto> GetUserActivitiesAsync(
        Guid userId, CancellationToken cancellationToken)
    {
        var readChapters = await _context.ReadingHistories.AsNoTracking()
            .Where(item => item.UserId == userId && item.ChapterId != Guid.Empty)
            .Select(item => new ActivityItemDto(item.ChapterId, item.ReadAt))
            .ToListAsync(cancellationToken);
        var comments = await _context.Comments.AsNoTracking()
            .Where(item => item.UserId == userId)
            .Select(item => new ActivityItemDto(item.Id, item.CreatedAt))
            .ToListAsync(cancellationToken);
        return new SocialActivitySnapshotDto { ReadChapters = readChapters, Comments = comments };
    }
}
