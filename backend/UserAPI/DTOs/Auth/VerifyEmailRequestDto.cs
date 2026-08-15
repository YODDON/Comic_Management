using System.ComponentModel.DataAnnotations;

namespace UserAPI.DTOs
{
    public class VerifyEmailRequestDto
    {
        [Required]
        public string Token { get; set; } = string.Empty;
    }
}
