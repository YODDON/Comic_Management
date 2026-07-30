using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SharedKernel.Utilities
{
    public static class SlugGenerator
    {
        public static string GenerateSlug(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            text = text.Replace('đ', 'd').Replace('Đ', 'D');
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            var cleanText = stringBuilder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
            
            cleanText = Regex.Replace(cleanText, @"[^a-z0-9\s-]", "");
            cleanText = Regex.Replace(cleanText, @"[\s-]+", "-").Trim('-');

            return cleanText;
        }
    }
}
