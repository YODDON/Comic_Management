using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ComicAPI.Application.DTOs
{
    public class CreateComicRequestDto : IValidatableObject
    {
        // Slug is optional - will be auto-generated from Title if not provided
        [MinLength(2, ErrorMessage = "Slug must be at least 2 characters.")]
        [MaxLength(255, ErrorMessage = "Slug cannot exceed 255 characters.")]
        [RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = "Slug must contain only lowercase letters, numbers, and hyphens.")]
        public string? Slug { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [MinLength(1, ErrorMessage = "Title must be at least 1 character.")]
        [MaxLength(500, ErrorMessage = "Title cannot exceed 500 characters.")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
        public string? Description { get; set; }

        public IFormFile? CoverImage { get; set; }

        [Url(ErrorMessage = "CoverUrl must be a valid URL.")]
        public string? CoverUrl { get; set; }

        [MaxLength(255, ErrorMessage = "Author cannot exceed 255 characters.")]
        public string? Author { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Unit price must be non-negative.")]
        public int UnitPrice { get; set; }

        [Range(0, 2, ErrorMessage = "Salary type must be 0, 1, or 2.")]
        public int? SalaryType { get; set; }

        public List<Guid>? CategoryIds { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (CoverImage == null && string.IsNullOrWhiteSpace(CoverUrl))
            {
                yield return new ValidationResult("A cover image file or cover URL is required.", new[] { nameof(CoverImage), nameof(CoverUrl) });
            }

            if (CoverImage != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = System.IO.Path.GetExtension(CoverImage.FileName).ToLowerInvariant();
                
                if (string.IsNullOrEmpty(extension) || Array.IndexOf(allowedExtensions, extension) < 0)
                {
                    yield return new ValidationResult("CoverImage must be a valid image file (.jpg, .jpeg, .png, .gif).", new[] { nameof(CoverImage) });
                }

                if (CoverImage.Length > 5 * 1024 * 1024)
                {
                    yield return new ValidationResult("CoverImage file size cannot exceed 5MB.", new[] { nameof(CoverImage) });
                }
            }
        }
    }
}
