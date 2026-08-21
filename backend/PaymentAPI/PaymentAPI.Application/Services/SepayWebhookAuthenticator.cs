using System.Security.Cryptography;
using System.Text;

namespace PaymentAPI.Services
{
    /// <summary>
    /// Authenticates callers of the SePay webhook by shared token.
    /// </summary>
    /// <remarks>
    /// This is the only thing standing between the internet and free coins: the webhook credits
    /// wallets, is AllowAnonymous, and is reachable publicly through ngrok. It must fail closed.
    /// </remarks>
    public static class SepayWebhookAuthenticator
    {
        /// <summary>SePay sends "Authorization: Apikey &lt;token&gt;". Bearer is tolerated too.</summary>
        private static readonly string[] KnownSchemes = { "Apikey ", "Bearer " };

        public static bool IsAuthorized(string? authorizationHeader, string? configuredKey)
        {
            // Fail closed. With no key configured we cannot authenticate anyone, so we trust no one.
            // The previous code skipped the check entirely when unconfigured, which meant an
            // unconfigured server accepted forged transfers and minted coins for free.
            if (string.IsNullOrWhiteSpace(configuredKey))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(authorizationHeader))
            {
                return false;
            }

            var presented = authorizationHeader.Trim();
            foreach (var scheme in KnownSchemes)
            {
                if (presented.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
                {
                    presented = presented[scheme.Length..].Trim();
                    break;
                }
            }

            // Whole-value equality, not Contains: a substring match would accept "xxx<token>xxx",
            // and would also accept a one-character token matching almost anything.
            // Constant-time so response timing cannot leak how much of the token was correct.
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(presented),
                Encoding.UTF8.GetBytes(configuredKey.Trim()));
        }
    }
}
