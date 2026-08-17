using System;
using System.ComponentModel.DataAnnotations;

namespace SocialAPI.DTOs
{
    public class CreateCommentDto
    {
        [Required]
        public Guid ComicId { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Content { get; set; } = string.Empty;

        public Guid? ParentCommentId { get; set; }
    }
}
