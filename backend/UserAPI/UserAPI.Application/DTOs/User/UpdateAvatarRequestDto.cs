using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace UserAPI.Application.DTOs;

public class UpdateAvatarRequestDto
{
    [Required]
    public IFormFile Avatar { get; set; } = null!;
}
