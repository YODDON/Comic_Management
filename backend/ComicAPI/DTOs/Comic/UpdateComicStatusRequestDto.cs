using System.ComponentModel.DataAnnotations;
using SharedKernel.Enums;

namespace ComicAPI.DTOs
{
    public class UpdateComicStatusRequestDto
    {
        [Required]
        public ComicStatus Status { get; set; }
    }
}
