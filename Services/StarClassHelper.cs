using System;

namespace EliteDataRelay.Services
{
    public readonly struct StarClassInfo
    {
        public string FriendlyName { get; }
        public bool IsScoopable { get; }
        public bool IsBoostStar { get; } // Neutron or White Dwarf
        public bool IsHazard { get; }    // Black hole, Neutron, White Dwarf, Wolf-Rayet

        public StarClassInfo(string friendlyName, bool scoopable, bool boost, bool hazard)
        {
            FriendlyName = friendlyName;
            IsScoopable = scoopable;
            IsBoostStar = boost;
            IsHazard = hazard;
        }
    }

    public static class StarClassHelper
    {
        public static StarClassInfo FromCode(string? starClass)
        {
            if (string.IsNullOrWhiteSpace(starClass))
                return new StarClassInfo("Unknown", false, false, false);

            var code = starClass.Trim();
            var upper = code.ToUpperInvariant();
            // Friendly name and attributes
            bool scoop = IsScoopable(upper);
            bool isNeutron = upper == "N" || upper.Contains("NEUTRON");
            bool isWhiteDwarf = upper.StartsWith("D");
            bool isBlackHole = upper == "H" || upper.Contains("BLACKHOLE");
            bool isWolfRayet = upper.StartsWith("W");
            bool hazard = isBlackHole || isNeutron || isWhiteDwarf || isWolfRayet;
            bool boost = isNeutron || isWhiteDwarf;

            string friendly = upper switch
            {
                "N" => "Neutron Star",
                "H" => "Black Hole",
                var s when s.StartsWith("D") => "White Dwarf",
                var s when s.StartsWith("W") => "Wolf-Rayet Star",
                "TTS" => "T Tauri Star",
                "AEBE" => "Herbig Ae/Be Star",
                _ => code // default to reported class
            };

            return new StarClassInfo(friendly, scoop, boost, hazard);
        }

        public static bool IsScoopable(string? starClass)
        {
            if (string.IsNullOrEmpty(starClass)) return false;
            char c = char.ToUpperInvariant(starClass[0]);
            return c == 'O' || c == 'B' || c == 'A' || c == 'F' || c == 'G' || c == 'K' || c == 'M';
        }
    }
}

