namespace SocialAPI.DTOs;

public class ReadingHistoryPageDto
{
    public List<ReadingHistoryDto> Items { get; set; } = [];
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
