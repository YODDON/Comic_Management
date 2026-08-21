using System;
using System.ComponentModel.DataAnnotations;
using SharedKernel.Enums;

namespace ChapterAPI.DTOs
{
    public class CreateChapterRequestDto
    {
        [Required]
        public Guid ComicId { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public int ChapterNumber { get; set; }

        public decimal UnitPrice { get; set; }

        [Required]
        public string Status { get; set; } = ChapterStatus.Draft.ToString();
    }
}
