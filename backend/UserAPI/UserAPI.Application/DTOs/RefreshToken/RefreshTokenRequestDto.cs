using System.ComponentModel.DataAnnotations;

namespace UserAPI.Application.DTOs
{
    public class RefreshTokenRequestDto
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
