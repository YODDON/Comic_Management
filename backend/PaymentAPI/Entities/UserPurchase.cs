using System;
using SharedKernel.Entities;

namespace PaymentAPI.Entities
{
    public class UserPurchase : BaseEntity
    {
        public int UserId { get; set; }
        public Guid ComicId { get; set; }
        public Guid ChapterId { get; set; }
        public decimal Price { get; set; }
        public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;
    }
}
