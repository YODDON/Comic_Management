using System.Collections.Generic;

namespace ChapterAPI.DTOs
{
    public class ChapterDetailDto : ChapterSummaryDto
    {
        public List<ChapterPageDto> ChapterPages { get; set; } = new List<ChapterPageDto>();
    }
}
