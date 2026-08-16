using System;
using System.Collections.Generic;

namespace ComicAPI.Application.DTOs
{
    public class ComicDetailDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string CoverUrl { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public int OwnerId { get; set; }
        public string Status { get; set; } = string.Empty;
        public int ViewCount { get; set; }
        public int ChapterCount { get; set; }
        public bool IsOutstanding { get; set; }
        public DateTime UpdatedAt { get; set; }

        public List<CategoryDto> Categories { get; set; } = new List<CategoryDto>();
    }
}
