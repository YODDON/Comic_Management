using System;

namespace ComicAPI.DTOs
{
    public class ComicSummaryDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string CoverUrl { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int ViewCount { get; set; }
        public bool IsOutstanding { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
