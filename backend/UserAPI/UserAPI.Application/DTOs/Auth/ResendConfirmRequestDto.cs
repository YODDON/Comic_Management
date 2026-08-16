using System.ComponentModel.DataAnnotations;

namespace UserAPI.Application.DTOs
{
    public class ResendConfirmRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
