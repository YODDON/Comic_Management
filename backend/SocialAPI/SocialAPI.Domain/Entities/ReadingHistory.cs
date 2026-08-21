using System;
using SharedKernel.Entities;

namespace SocialAPI.Entities
{
    public class ReadingHistory : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid ComicId { get; set; }
        public Guid ChapterId { get; set; }
        public DateTime ReadAt { get; set; }
    }
}
