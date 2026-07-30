using System;

namespace SocialAPI.DTOs
{
    public class ReadingHistoryDto
    {
        public Guid Id { get; set; }
        public Guid ComicId { get; set; }
        public Guid ChapterId { get; set; }
        public DateTime ReadAt { get; set; }
    }
}
