using SharedKernel.Enums;
using SocialAPI.DTOs;
using SocialAPI.Entities;
using SocialAPI.Interfaces;

namespace SocialAPI.Services;

public class ReadingHistoryService : IReadingHistoryService
{
    private readonly IReadingHistoryRepository _repository;
    private readonly IMissionProgressNotifier _missionProgressNotifier;

    public ReadingHistoryService(
        IReadingHistoryRepository repository,
        IMissionProgressNotifier missionProgressNotifier)
    {
        _repository = repository;
        _missionProgressNotifier = missionProgressNotifier;
    }

    public async Task<ReadingHistoryPageDto> GetMineAsync(Guid userId, int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var (items, totalCount) = await _repository.GetByUserAsync(userId, page, pageSize);
        return new ReadingHistoryPageDto
        {
            Items = items.Select(Map).ToList(),
            Total = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ReadingHistoryDto> RecordAsync(
        int numericUserId, Guid userId, RecordReadingHistoryDto request)
    {
        var item = await _repository.GetForUpdateAsync(userId, request.ComicId);
        if (item is null)
        {
            item = new ReadingHistory { UserId = userId, ComicId = request.ComicId };
            _repository.Add(item);
        }

        item.ChapterId = request.ChapterId;
        item.ReadAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;
        await _missionProgressNotifier.RecordAsync(
            numericUserId, MissionType.ReadChapter, request.ChapterId, item.ReadAt);
        await _repository.SaveChangesAsync();
        return Map(item);
    }

    private static ReadingHistoryDto Map(ReadingHistory item) => new()
    {
        Id = item.Id,
        ComicId = item.ComicId,
        ChapterId = item.ChapterId,
        ReadAt = item.ReadAt
    };
}
