using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SharedKernel.Enums;

namespace ComicAPI.DTOs
{
    public class UpdateComicRequestDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public List<Guid> CategoryIds { get; set; } = new List<Guid>();

        [MaxLength(255)]
        public string? Author { get; set; }

        [Url(ErrorMessage = "ThumbnailUrl must be a valid URL.")]
        public string? ThumbnailUrl { get; set; }

        public ComicStatus? Status { get; set; }
    }
}
