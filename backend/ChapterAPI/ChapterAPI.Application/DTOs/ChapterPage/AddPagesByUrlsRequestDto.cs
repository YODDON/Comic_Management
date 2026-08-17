using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ChapterAPI.DTOs
{
    public class AddPagesByUrlsRequestDto
    {
        [Required]
        [MinLength(1)]
        public List<string> Urls { get; set; } = new();
    }
}
