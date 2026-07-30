using System.ComponentModel.DataAnnotations;

namespace UserAPI.DTOs
{
    public class GoogleLoginRequestDto
    {
        [Required]
        public string IdToken { get; set; } = string.Empty;
    }
}
