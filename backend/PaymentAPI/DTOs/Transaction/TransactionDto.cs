using System;
using SharedKernel.Enums;

namespace PaymentAPI.DTOs
{
    public class TransactionDto
    {
        public Guid Id { get; set; }
        public int UserId { get; set; }
        public TransactionType Type { get; set; }
        public decimal Amount { get; set; }
        public string CurrencyType { get; set; } = string.Empty;
        public TransactionStatus Status { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string TransactionCode { get; set; } = string.Empty;
        public Guid? ChapterId { get; set; }
        public Guid? ComicId { get; set; }
        public string? ChapterTitle { get; set; }
        public int? ChapterNumber { get; set; }
        public string? ChapterSlug { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
