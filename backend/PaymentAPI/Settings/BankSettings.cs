namespace PaymentAPI.Settings
{
    /// <summary>
    /// Receiving bank account (for the VietQR image) and the SePay webhook token.
    /// </summary>
    public class BankSettings
    {
        public const string SectionName = "BankSettings";

        /// <summary>VietQR bank code, e.g. VCB / MB / ACB, or the numeric BIN.</summary>
        public string BankCode { get; set; } = string.Empty;

        /// <summary>Account number. MUST be the exact account SePay monitors, or transfers arrive unreported.</summary>
        public string AccountNumber { get; set; } = string.Empty;

        public string AccountName { get; set; } = string.Empty;

        /// <summary>
        /// Shared token SePay presents on the webhook. Without it no caller can be authenticated,
        /// so the webhook rejects everything — see <see cref="IsWebhookConfigured"/>.
        /// </summary>
        public string SepayApiKey { get; set; } = string.Empty;

        public bool IsWebhookConfigured => !string.IsNullOrWhiteSpace(SepayApiKey);

        public bool IsBankConfigured =>
            !string.IsNullOrWhiteSpace(BankCode) && !string.IsNullOrWhiteSpace(AccountNumber);
    }
}
