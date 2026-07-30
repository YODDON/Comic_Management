using System.ComponentModel.DataAnnotations;

namespace ComicAPI.DTOs
{
    public class CreateCategoryRequestDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Tag { get; set; } = string.Empty;
    }
}
