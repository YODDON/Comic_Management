using System;
using SharedKernel.Entities;
using SharedKernel.Enums;

namespace PaymentAPI.Entities
{
    public class Transaction : BaseEntity
    {
        public int UserId { get; set; }
        public Guid? ChapterId { get; set; }
        public TransactionType Type { get; set; }
        public decimal Amount { get; set; }
        public string CurrencyType { get; set; } = string.Empty;
        public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
        public string? Note { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string TransactionCode { get; set; } = string.Empty;
    }
}
