namespace PaymentAPI.Settings
{
    /// <summary>
    /// Settings that apply to every top-up source (VietQR + admin approval, and any future gateway).
    /// </summary>
    /// <remarks>
    /// CoinRate previously lived in VnpaySettings, which was wrong: the credit path is shared by all
    /// top-up sources, so a VietQR deposit depended on VNPay-specific config. Removing VNPay forced
    /// the fix.
    /// </remarks>
    public class TopUpSettings
    {
        public const string SectionName = "TopUp";

        /// <summary>Coins credited per 1 VND. 1 = coin and VND are interchangeable.</summary>
        public decimal CoinRate { get; set; } = 1m;
    }
}
