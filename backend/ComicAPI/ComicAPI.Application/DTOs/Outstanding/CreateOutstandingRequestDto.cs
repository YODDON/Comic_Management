using System;
using System.ComponentModel.DataAnnotations;

namespace ComicAPI.Application.DTOs
{
    public class CreateOutstandingRequestDto
    {
        [Required]
        public Guid ComicId { get; set; }
        public int Priority { get; set; } = 0;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
