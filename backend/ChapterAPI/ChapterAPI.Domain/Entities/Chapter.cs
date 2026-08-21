using System;
using System.Collections.Generic;
using SharedKernel.Entities;

namespace ChapterAPI.Entities
{
    public class Chapter : BaseEntity
    {
        public Guid ComicId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public int ChapterNumber { get; set; }
        public string ThumbnailUrl { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int PageCount { get; set; }
        public string Status { get; set; } = "Active";

        public virtual ICollection<ChapterPage> ChapterPages { get; set; } = new List<ChapterPage>();
        public virtual ICollection<UserPurchase> UserPurchases { get; set; } = new List<UserPurchase>();
    }
}
