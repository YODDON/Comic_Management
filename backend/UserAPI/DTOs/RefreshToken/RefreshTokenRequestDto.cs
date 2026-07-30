using System.ComponentModel.DataAnnotations;

namespace UserAPI.DTOs
{
    public class RefreshTokenRequestDto
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
