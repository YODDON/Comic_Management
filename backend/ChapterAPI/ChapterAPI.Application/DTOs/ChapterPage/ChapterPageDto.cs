using System;

namespace ChapterAPI.DTOs
{
    public class ChapterPageDto
    {
        public Guid Id { get; set; }
        public int PageNumber { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
    }
}
