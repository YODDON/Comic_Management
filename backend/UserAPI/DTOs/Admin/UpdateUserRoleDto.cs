using System.ComponentModel.DataAnnotations;

namespace UserAPI.DTOs;

public class UpdateUserRoleDto
{
    [Required(ErrorMessage = "Role là bắt buộc.")]
    public string Role { get; set; } = string.Empty;
}
