using System;
using System.Globalization;
using System.Text;

namespace EliteDataRelay.Models.Mining
{
    internal static class MiningNameHelper
    {
        public static string NormalizeName(string? rawName, string? localized = null)
        {
            if (!string.IsNullOrWhiteSpace(localized))
            {
                return localized!;
            }

            if (string.IsNullOrWhiteSpace(rawName))
            {
                return "Unknown";
            }

            var name = Cleanup(rawName);
            if (string.IsNullOrWhiteSpace(name))
            {
                return "Unknown";
            }

            var lower = name.ToLowerInvariant();
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(lower);
        }

        private static string Cleanup(string raw)
        {
            var value = raw.Trim();
            if (value.StartsWith("$", StringComparison.Ordinal))
            {
                value = value.TrimStart('$');
            }

            if (value.EndsWith("_name;", StringComparison.OrdinalIgnoreCase))
            {
                value = value[..^6];
            }
            else if (value.EndsWith(";"))
            {
                value = value[..^1];
            }

            value = value.Replace("_", " ", StringComparison.Ordinal);
            if (value.Contains(" ", StringComparison.Ordinal))
            {
                return value;
            }

            return SplitCamelCase(value);
        }

        private static string SplitCamelCase(string input)
        {
            var sb = new StringBuilder(input.Length * 2);
            for (int i = 0; i < input.Length; i++)
            {
                var c = input[i];
                if (i > 0 && char.IsUpper(c) && !char.IsWhiteSpace(input[i - 1]))
                {
                    sb.Append(' ');
                }
                sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
