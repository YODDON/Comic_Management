using System.ComponentModel.DataAnnotations;

namespace BannerAPI.DTOs;

public class UpdateBannerRequestDto
{
    [MaxLength(200, ErrorMessage = "Tiêu đề banner không được vượt quá 200 ký tự.")]
    [RegularExpression(@"^[\p{L}\p{N} ]+$", ErrorMessage = "Tiêu đề chỉ được chứa chữ, số và khoảng trắng.")]
    public string? Title { get; set; }

    [Required, Url, MaxLength(2048)]
    public string ImageUrl { get; set; } = string.Empty;

    [Url, MaxLength(2048)]
    public string? LinkUrl { get; set; }

    [MaxLength(500)]
    public string? ImagePublicId { get; set; }

    public bool IsActive { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Thứ tự hiển thị phải là số nguyên từ 0 trở lên.")]
    public int DisplayOrder { get; set; }
}
