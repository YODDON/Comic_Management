using System.ComponentModel.DataAnnotations;

namespace UserAPI.DTOs;

public class UpdateAvatarRequestDto
{
    [Required]
    public IFormFile Avatar { get; set; } = null!;
}
