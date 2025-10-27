using System;
using System.IO;
using System.Text.Json;
using EliteDataRelay.Configuration;
using EliteDataRelay.UI;

namespace EliteDataRelay.Services
{
    /// <summary>
    /// Exports overlay positions to a JSON file that can be used to position OBS sources.
    /// This helps streamers/recorders match their OBS window capture positions to the actual overlay positions.
    /// </summary>
    public static class ObsPositionExporter
    {
        private static readonly string PositionFilePath = Path.Combine(AppConfiguration.OutputDirectory, "overlay_positions.json");

        public class OverlayPositionData
        {
            public OverlayInfo? Info { get; set; }
            public OverlayInfo? Cargo { get; set; }
            public OverlayInfo? ShipIcon { get; set; }
            public OverlayInfo? Exploration { get; set; }
        }

        public class OverlayInfo
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public string WindowTitle { get; set; } = "";
            public string Instructions { get; set; } = "";
        }

        /// <summary>
        /// Exports the current overlay positions to a JSON file.
        /// </summary>
        public static void ExportPositions(OverlayForm? infoOverlay, OverlayForm? cargoOverlay, OverlayForm? shipIconOverlay, OverlayForm? explorationOverlay)
        {
            try
            {
                var data = new OverlayPositionData();

                if (infoOverlay != null && infoOverlay.Visible)
                {
                    data.Info = new OverlayInfo
                    {
                        X = infoOverlay.Location.X,
                        Y = infoOverlay.Location.Y,
                        Width = infoOverlay.Width,
                        Height = infoOverlay.Height,
                        WindowTitle = "Elite Data Relay: Info",
                        Instructions = $"In OBS: Add Window Capture source → Select '{infoOverlay.Text}' → Right-click source in scene → Transform → Edit Transform → Set Position to X:{infoOverlay.Location.X} Y:{infoOverlay.Location.Y}"
                    };
                }

                if (cargoOverlay != null && cargoOverlay.Visible)
                {
                    data.Cargo = new OverlayInfo
                    {
                        X = cargoOverlay.Location.X,
                        Y = cargoOverlay.Location.Y,
                        Width = cargoOverlay.Width,
                        Height = cargoOverlay.Height,
                        WindowTitle = "Elite Data Relay: Cargo",
                        Instructions = $"In OBS: Add Window Capture source → Select '{cargoOverlay.Text}' → Right-click source in scene → Transform → Edit Transform → Set Position to X:{cargoOverlay.Location.X} Y:{cargoOverlay.Location.Y}"
                    };
                }

                if (shipIconOverlay != null && shipIconOverlay.Visible)
                {
                    data.ShipIcon = new OverlayInfo
                    {
                        X = shipIconOverlay.Location.X,
                        Y = shipIconOverlay.Location.Y,
                        Width = shipIconOverlay.Width,
                        Height = shipIconOverlay.Height,
                        WindowTitle = "Elite Data Relay: Ship Icon",
                        Instructions = $"In OBS: Add Window Capture source → Select '{shipIconOverlay.Text}' → Right-click source in scene → Transform → Edit Transform → Set Position to X:{shipIconOverlay.Location.X} Y:{shipIconOverlay.Location.Y}"
                    };
                }

                if (explorationOverlay != null && explorationOverlay.Visible)
                {
                    data.Exploration = new OverlayInfo
                    {
                        X = explorationOverlay.Location.X,
                        Y = explorationOverlay.Location.Y,
                        Width = explorationOverlay.Width,
                        Height = explorationOverlay.Height,
                        WindowTitle = "Elite Data Relay: Exploration",
                        Instructions = $"In OBS: Add Window Capture source → Select '{explorationOverlay.Text}' → Right-click source in scene → Transform → Edit Transform → Set Position to X:{explorationOverlay.Location.X} Y:{explorationOverlay.Location.Y}"
                    };
                }

                // Ensure output directory exists
                var directory = Path.GetDirectoryName(PositionFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(data, options);
                File.WriteAllText(PositionFilePath, json);

                System.Diagnostics.Trace.WriteLine($"[ObsPositionExporter] Overlay positions exported to: {PositionFilePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"[ObsPositionExporter] Error exporting positions: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the path to the exported positions file.
        /// </summary>
        public static string GetPositionFilePath() => PositionFilePath;
    }
}
