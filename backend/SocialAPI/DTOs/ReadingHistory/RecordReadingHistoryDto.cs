using System;
using System.ComponentModel.DataAnnotations;

namespace SocialAPI.DTOs
{
    public class RecordReadingHistoryDto
    {
        [Required]
        public Guid ComicId { get; set; }

        [Required]
        public Guid ChapterId { get; set; }
    }
}
