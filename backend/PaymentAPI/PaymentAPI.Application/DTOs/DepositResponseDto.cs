using System;

namespace PaymentAPI.DTOs
{
    public class DepositResponseDto
    {
        public Guid TransactionId { get; set; }
        public string TransactionCode { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string QrUrl { get; set; } = string.Empty;
    }
}
