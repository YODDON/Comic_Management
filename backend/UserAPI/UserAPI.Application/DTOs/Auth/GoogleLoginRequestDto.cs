using System.ComponentModel.DataAnnotations;

namespace UserAPI.Application.DTOs
{
    public class GoogleLoginRequestDto
    {
        [Required]
        public string IdToken { get; set; } = string.Empty;
    }
}
