using System.ComponentModel.DataAnnotations;

namespace UserAPI.DTOs;

public class UpdateProfileRequestDto
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Username { get; set; } = string.Empty;
}
