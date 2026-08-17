using System.Globalization;
using System.Text.RegularExpressions;

namespace PaymentAPI.Services
{
    /// <summary>
    /// Generates the deposit reference the payer puts in their bank-transfer content, and recognises
    /// it again when SePay reports the transfer.
    /// </summary>
    /// <remarks>
    /// Generation and matching live together on purpose. They were previously written in two places
    /// and silently drifted: the generator emitted UP + 15 digits while the webhook matched
    /// <c>UP\d{12}</c>, so every code was truncated to its first 12 digits, the lookup found nothing,
    /// and no deposit could ever be credited. Both now derive from the same constants, so the pattern
    /// cannot disagree with the generator again.
    /// </remarks>
    public static class DepositCodeGenerator
    {
        private const string Prefix = "UP";
        private const string TimestampFormat = "yyMMddHHmmss";
        private const int RandomDigits = 6;

        private static readonly int DigitCount = TimestampFormat.Length + RandomDigits;

        /// <summary>
        /// The digit count comes from the same constants the generator uses, so the two cannot drift.
        /// The trailing (?!\d) matters: without it, a longer digit run would match a *prefix* of
        /// itself and resolve to the wrong transaction — the same class of bug being fixed here.
        /// </summary>
        private static readonly Regex Pattern =
            new($@"{Prefix}\d{{{DigitCount}}}(?!\d)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static string Generate()
        {
            var timestamp = DateTime.UtcNow.ToString(TimestampFormat, CultureInfo.InvariantCulture);

            // Zero-padded so the length is always exactly RandomDigits — a variable-length code would
            // break the fixed-width pattern above.
            // Random.Shared, not new Random(): the latter is time-seeded, so two calls in the same
            // tick produce identical codes, and TransactionCode is unique-indexed.
            var random = Random.Shared.Next(0, 1_000_000).ToString($"D{RandomDigits}", CultureInfo.InvariantCulture);

            return $"{Prefix}{timestamp}{random}";
        }

        /// <summary>Extracts the deposit code from a bank transfer's content line.</summary>
        public static bool TryExtract(string? transferContent, out string code)
        {
            code = string.Empty;

            if (string.IsNullOrWhiteSpace(transferContent))
            {
                return false;
            }

            var match = Pattern.Match(transferContent);
            if (!match.Success)
            {
                return false;
            }

            code = match.Value;
            return true;
        }
    }
}
