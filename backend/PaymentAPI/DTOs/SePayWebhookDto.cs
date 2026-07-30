namespace PaymentAPI.DTOs
{
    /// <summary>
    /// SePay's bank-transaction webhook payload.
    /// </summary>
    /// <remarks>
    /// Every field is nullable on purpose. Under [ApiController], a non-nullable reference-type
    /// property is treated as required, so a null from the sender fails model validation with 400
    /// BEFORE the action runs. SePay legitimately sends null for several fields (e.g. "code": null on
    /// a plain bank transfer), which would reject the whole webhook and never credit the deposit.
    /// A webhook receiver must be liberal in what it accepts, so nothing here is required.
    /// </remarks>
    public class SePayWebhookDto
    {
        public long Id { get; set; }
        public string? Gateway { get; set; }
        public string? TransactionDate { get; set; }
        public string? AccountNumber { get; set; }
        public string? SubAccount { get; set; }
        public string? Code { get; set; }
        public string? Content { get; set; }
        public string? TransferType { get; set; }
        public decimal TransferAmount { get; set; }
        public decimal Accumulated { get; set; }
        public string? ReferenceCode { get; set; }
        public string? Description { get; set; }
    }
}
