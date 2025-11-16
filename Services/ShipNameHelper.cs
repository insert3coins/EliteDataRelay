using System.Globalization;
using System.Linq;
using System.Text;

namespace EliteDataRelay.Services
{
    internal static class ShipNameHelper
    {
        public static string GetDisplayName(string? internalName)
        {
            if (string.IsNullOrWhiteSpace(internalName))
            {
                return "Unknown";
            }

            var name = internalName.Replace('_', ' ').Trim();
            var builder = new StringBuilder(name.Length * 2);
            char previous = '\0';
            foreach (var c in name)
            {
                if (NeedsSpace(previous, c))
                {
                    builder.Append(' ');
                }
                builder.Append(c);
                previous = c;
            }

            var formatted = builder.ToString().Trim();
            if (formatted.Length == 0)
            {
                return "Unknown";
            }

            var textInfo = CultureInfo.InvariantCulture.TextInfo;
            var titleCase = textInfo.ToTitleCase(formatted.ToLowerInvariant());
            var words = titleCase.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length; i++)
            {
                var word = words[i];
                if (IsRomanNumeral(word))
                {
                    words[i] = word.ToUpperInvariant();
                }
                else if (string.Equals(word, "Mk", StringComparison.OrdinalIgnoreCase))
                {
                    words[i] = "Mk";
                }
            }

            return string.Join(' ', words);
        }

        private static bool NeedsSpace(char previous, char current)
        {
            if (previous == '\0' || previous == ' ')
            {
                return false;
            }

            if (char.IsUpper(current) && char.IsLower(previous))
            {
                return true;
            }

            if (char.IsDigit(current) && !char.IsDigit(previous))
            {
                return true;
            }

            return false;
        }

        private static bool IsRomanNumeral(string value)
        {
            return value.All(c =>
                c == 'i' || c == 'I' ||
                c == 'v' || c == 'V' ||
                c == 'x' || c == 'X');
        }
    }
}
