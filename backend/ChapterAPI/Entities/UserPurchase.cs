using System;
using SharedKernel.Entities;

namespace ChapterAPI.Entities
{
    public class UserPurchase : BaseEntity
    {
        public int UserId { get; set; }
        public Guid ChapterId { get; set; }
        public DateTime PurchasedAt { get; set; }

        public virtual Chapter Chapter { get; set; } = null!;
    }
}
