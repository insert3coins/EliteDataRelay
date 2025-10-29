using System;

namespace EliteDataRelay.Models
{
    public class MiningSessionPreferences
    {
        public bool AutoStartOnProspector { get; set; } = true;
        public bool CargoFullPromptEnabled { get; set; } = true;
        public double CargoIdleMinutesThreshold { get; set; } = 1.0;
        public int PreferredFireGroup { get; set; } = 1;
        public bool AnnouncementsEnabled { get; set; } = true;
        public bool AutoGenerateHtmlReports { get; set; }
            = false;

        public MiningSessionPreferences Clone() => new()
        {
            AutoStartOnProspector = AutoStartOnProspector,
            CargoFullPromptEnabled = CargoFullPromptEnabled,
            CargoIdleMinutesThreshold = CargoIdleMinutesThreshold,
            PreferredFireGroup = PreferredFireGroup,
            AnnouncementsEnabled = AnnouncementsEnabled,
            AutoGenerateHtmlReports = AutoGenerateHtmlReports
        };

        public void ApplyFrom(MiningSessionPreferences preferences)
        {
            if (preferences == null) throw new ArgumentNullException(nameof(preferences));

            AutoStartOnProspector = preferences.AutoStartOnProspector;
            CargoFullPromptEnabled = preferences.CargoFullPromptEnabled;
            CargoIdleMinutesThreshold = preferences.CargoIdleMinutesThreshold;
            PreferredFireGroup = preferences.PreferredFireGroup;
            AnnouncementsEnabled = preferences.AnnouncementsEnabled;
            AutoGenerateHtmlReports = preferences.AutoGenerateHtmlReports;
        }
    }
}

