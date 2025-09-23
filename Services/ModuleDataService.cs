using EliteDataRelay.Models;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EliteDataRelay.Services
{
    public static class ModuleDataService
    {
        // A simple regex to extract size and class from core/optional module names
        private static readonly Regex SizeAndClassRegex = new Regex(@"_size(\d+)_rating([a-i])", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string GetModuleDisplayName(ShipModule module)
        {
            // The journal 'Item' is an internal, lowercase name. We'll format it.
            string name = module.Item;

            // Strip prefixes
            if (name.StartsWith("hpt_")) name = name.Substring(4);
            if (name.StartsWith("int_")) name = name.Substring(4);

            // Strip suffixes like size/class/mount type for the base name
            var match = SizeAndClassRegex.Match(name);
            if (match.Success)
            {
                name = name.Substring(0, match.Index);
            }
            name = name.Replace("_", " ").Trim();

            // Special cases and capitalization
            if (name.Contains("fsdinterdictor")) name = name.Replace("fsdinterdictor", "FSD Interdictor");
            if (name.Contains("fsd")) name = name.Replace("fsd", "FSD");
            if (name.Contains("srv")) name = name.Replace("srv", "SRV");

            return ToTitleCase(name);
        }

        public static string GetModuleDetails(ShipModule module)
        {
            string item = module.Item.ToLowerInvariant();
            var sizeAndClassMatch = SizeAndClassRegex.Match(item);

            if (sizeAndClassMatch.Success)
            {
                string size = sizeAndClassMatch.Groups[1].Value;
                string rating = sizeAndClassMatch.Groups[2].Value.ToUpper();
                return $"({size}{rating})";
            }

            // Handle hardpoint sizes and types
            string sizeText = GetHardpointSize(item);
            string mountText = GetHardpointMount(item);

            if (!string.IsNullOrEmpty(sizeText) || !string.IsNullOrEmpty(mountText))
            {
                return $"({string.Join(" ", new[] { sizeText, mountText }.Where(s => !string.IsNullOrEmpty(s)))})";
            }

            return string.Empty; // No details to show
        }

        private static string GetHardpointSize(string item)
        {
            if (item.Contains("_small")) return "Small";
            if (item.Contains("_medium")) return "Medium";
            if (item.Contains("_large")) return "Large";
            if (item.Contains("_huge")) return "Huge";
            return string.Empty;
        }

        private static string GetHardpointMount(string item)
        {
            if (item.Contains("_fixed")) return "Fixed";
            if (item.Contains("_gimbal")) return "Gimballed";
            if (item.Contains("_turret")) return "Turreted";
            return string.Empty;
        }

        public static string GetEngineeringInfo(ShipModule module)
        {
            if (module.Engineering == null)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            sb.Append($"G{module.Engineering.Level} {module.Engineering.BlueprintName}");

            if (!string.IsNullOrEmpty(module.Engineering.ExperimentalEffect_Localised))
            {
                sb.Append($", {module.Engineering.ExperimentalEffect_Localised}");
            }

            return sb.ToString();
        }

        private static string ToTitleCase(string str) =>
            System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str.ToLower()).Replace("Fsd", "FSD").Replace("Srv", "SRV");
    }
}