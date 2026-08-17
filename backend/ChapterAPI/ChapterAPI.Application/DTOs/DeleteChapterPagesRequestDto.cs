using System.ComponentModel.DataAnnotations;

namespace ChapterAPI.DTOs;

public class DeleteChapterPagesRequestDto
{
    [Required]
    [MinLength(1, ErrorMessage = "Phải chọn ít nhất một trang ảnh để xóa.")]
    public List<Guid> PageIds { get; set; } = [];
}
