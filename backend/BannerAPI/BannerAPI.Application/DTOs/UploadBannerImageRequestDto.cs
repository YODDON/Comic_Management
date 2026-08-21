using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace BannerAPI.DTOs;

public class UploadBannerImageRequestDto
{
    [Required]
    public IFormFile File { get; set; } = null!;
}
