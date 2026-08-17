using System;

namespace ChapterAPI.DTOs
{
    public class ChapterSummaryDto
    {
        public Guid Id { get; set; }
        public Guid ComicId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public int ChapterNumber { get; set; }
        public string ThumbnailUrl { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public bool IsPurchased { get; set; }
        public int PageCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
