using System.ComponentModel.DataAnnotations;

namespace UserAPI.DTOs
{
    public class ResendConfirmRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
