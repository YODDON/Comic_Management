using System.ComponentModel.DataAnnotations;

namespace UserAPI.Application.DTOs
{
    public class LogoutRequestDto
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
