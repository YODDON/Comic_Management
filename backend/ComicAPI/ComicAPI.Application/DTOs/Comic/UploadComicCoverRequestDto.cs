using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ComicAPI.Application.DTOs;

public class UploadComicCoverRequestDto : IValidatableObject
{
    [Required]
    public IFormFile File { get; set; } = null!;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (File is null || File.Length == 0)
        {
            yield return new ValidationResult("Vui lòng chọn file ảnh.", [nameof(File)]);
            yield break;
        }

        if (File.Length > 5 * 1024 * 1024)
        {
            yield return new ValidationResult("Ảnh bìa không được vượt quá 5 MB.", [nameof(File)]);
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        var extension = Path.GetExtension(File.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            yield return new ValidationResult(
                "Chỉ hỗ trợ ảnh JPG, JPEG, PNG, WebP hoặc GIF.",
                [nameof(File)]);
        }
    }
}
