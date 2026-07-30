using System.ComponentModel.DataAnnotations;

namespace UserAPI.DTOs
{
    public class LogoutRequestDto
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
