using System.Collections.Generic;

namespace EliteDataRelay.Models
{
    public class StationInfoData
    {
        public string StationName { get; set; } = "N/A";
        public string StationType { get; set; } = "N/A";
        public string Allegiance { get; set; } = "N/A";
        public string Economy { get; set; } = "N/A";
        public string Government { get; set; } = "N/A";
        public string ControllingFaction { get; set; } = "N/A";

        public bool HasRefuel { get; set; }
        public bool HasRepair { get; set; }
        public bool HasRearm { get; set; }
        public bool HasOutfitting { get; set; }
        public bool HasShipyard { get; set; }
        public bool HasMarket { get; set; }
    }
}
