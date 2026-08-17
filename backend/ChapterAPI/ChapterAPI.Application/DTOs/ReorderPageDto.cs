using System;

namespace ChapterAPI.DTOs
{
    public class ReorderPageDto
    {
        public Guid PageId { get; set; }
        public int NewOrder { get; set; }
    }
}
