using SocialAPI.DTOs;

namespace SocialAPI.Interfaces;

public interface IReadingHistoryService
{
    Task<ReadingHistoryPageDto> GetMineAsync(Guid userId, int page, int pageSize);
    Task<ReadingHistoryDto> RecordAsync(int numericUserId, Guid userId, RecordReadingHistoryDto request);
}
