namespace EliteDataRelay.Models
{
    public class SystemInfoData
    {
        public string SystemName { get; set; } = "Unknown System";
        public string Allegiance { get; set; } = "N/A";
        public string Government { get; set; } = "N/A";
        public string Economy { get; set; } = "N/A";
        public string Security { get; set; } = "N/A";
        public long Population { get; set; } = 0;
        public string ControllingFaction { get; set; } = "N/A";
        public string FactionState { get; set; } = "N/A";
    }
}