using System;
using SharedKernel.Entities;

namespace ChapterAPI.Entities
{
    public class ChapterPage : BaseEntity
    {
        public Guid ChapterId { get; set; }
        public int PageNumber { get; set; }
        public string ImageUrl { get; set; } = string.Empty;

        public virtual Chapter Chapter { get; set; } = null!;
    }
}
