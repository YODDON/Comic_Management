using System.ComponentModel.DataAnnotations;
using SharedKernel.Enums;

namespace ComicAPI.Application.DTOs
{
    public class UpdateComicStatusRequestDto
    {
        [Required]
        public ComicStatus Status { get; set; }
    }
}
