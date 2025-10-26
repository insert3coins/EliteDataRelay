using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using EliteDataRelay.Configuration;
using EliteDataRelay.Models;

namespace EliteDataRelay.UI
{
    public partial class OverlayForm
    {
        public void UpdateExplorationData(SystemExplorationData? systemData)
        {
            if (_position != OverlayPosition.Exploration) return;

            _currentExplorationData = systemData;

            if (this.InvokeRequired)
            {
                this.Invoke(new System.Action(() => UpdateExplorationData(systemData)));
                return;
            }

            if (systemData == null || string.IsNullOrEmpty(systemData.SystemName))
            {
                _explorationSystemLabel.Text = "No System";
                _explorationBodiesLabel.Text = "Bodies: 0/0";
                _explorationMappedLabel.Text = "Mapped: 0";
                _explorationFirstsLabel.Text = "";
                _explorationNotableBodiesPanel.Controls.Clear();
                return;
            }

            // Update system info
            _explorationSystemLabel.Text = systemData.SystemName;

            var bodiesText = systemData.TotalBodies > 0
                ? $"Bodies: {systemData.ScannedBodies}/{systemData.TotalBodies}"
                : $"Bodies: {systemData.ScannedBodies}";

            if (systemData.FSSProgress > 0 && systemData.FSSProgress < 100)
            {
                bodiesText += $" (FSS: {systemData.FSSProgress:F0}%)";
            }

            _explorationBodiesLabel.Text = bodiesText;
            _explorationMappedLabel.Text = $"Mapped: {systemData.MappedBodies}";

            // Count first discoveries (bodies that weren't previously discovered)
            var firstDiscoveries = systemData.Bodies.Count(b => !b.WasDiscovered);
            var firstMappings = systemData.Bodies.Count(b => b.IsMapped && !b.WasMapped);

            if (firstDiscoveries > 0 || firstMappings > 0)
            {
                var firsts = new List<string>();
                if (firstDiscoveries > 0) firsts.Add($"⭐ {firstDiscoveries} First");
                if (firstMappings > 0) firsts.Add($"🗺️ {firstMappings} Mapped");
                _explorationFirstsLabel.Text = string.Join(" | ", firsts);
                _explorationFirstsLabel.ForeColor = Color.FromArgb(34, 139, 34); // Green for first discoveries
            }
            else if (systemData.Bodies.Any())
            {
                // All scanned bodies were already discovered/mapped
                _explorationFirstsLabel.Text = "Known System";
                _explorationFirstsLabel.ForeColor = SystemColors.GrayText;
            }
            else
            {
                _explorationFirstsLabel.Text = "";
            }

            Debug.WriteLine($"[OverlayForm.Exploration] UpdateExplorationData called - Mapped: {systemData.MappedBodies}, Scanned: {systemData.ScannedBodies}");

            // Update notable bodies
            UpdateNotableBodiesPanel(systemData);
        }

        private void UpdateNotableBodiesPanel(SystemExplorationData systemData)
        {
            _explorationNotableBodiesPanel.SuspendLayout();
            _explorationNotableBodiesPanel.Controls.Clear();

            int yPosition = 0;
            const int lineHeight = 18;

            // Count notable body types
            int earthLikes = 0;
            int waterWorlds = 0;
            int ammoniaWorlds = 0;
            int terraformables = 0;
            int biologicals = 0;

            foreach (var body in systemData.Bodies)
            {
                var bodyType = body.BodyType.ToLowerInvariant();

                if (bodyType.Contains("earth") || bodyType.Contains("earthlike"))
                    earthLikes++;
                else if (bodyType.Contains("water"))
                    waterWorlds++;
                else if (bodyType.Contains("ammonia"))
                    ammoniaWorlds++;

                if (!string.IsNullOrEmpty(body.TerraformState) && body.TerraformState != "Not Terraformable")
                    terraformables++;

                if (body.BiologicalSignals.Any())
                    biologicals++;
            }

            // Display notable bodies
            if (earthLikes > 0)
            {
                AddNotableBodyLabel("🌍 Earth-like World", earthLikes, yPosition, Color.FromArgb(34, 139, 34));
                yPosition += lineHeight;
            }

            if (waterWorlds > 0)
            {
                AddNotableBodyLabel("💧 Water World", waterWorlds, yPosition, Color.FromArgb(0, 119, 190));
                yPosition += lineHeight;
            }

            if (ammoniaWorlds > 0)
            {
                AddNotableBodyLabel("⚗️ Ammonia World", ammoniaWorlds, yPosition, Color.FromArgb(138, 43, 226));
                yPosition += lineHeight;
            }

            if (terraformables > 0)
            {
                AddNotableBodyLabel("⚡ Terraformable", terraformables, yPosition, Color.FromArgb(255, 165, 0));
                yPosition += lineHeight;
            }

            if (biologicals > 0)
            {
                AddNotableBodyLabel("🧬 Biologicals", biologicals, yPosition, Color.FromArgb(16, 185, 129));
                yPosition += lineHeight;
            }

            if (yPosition == 0)
            {
                var noneLabel = new Label
                {
                    Text = "None found",
                    ForeColor = SystemColors.GrayText,
                    Font = _listFont,
                    BackColor = Color.Transparent,
                    AutoSize = true,
                    Location = new Point(0, 0)
                };
                _explorationNotableBodiesPanel.Controls.Add(noneLabel);
            }

            _explorationNotableBodiesPanel.ResumeLayout();
        }

        private void AddNotableBodyLabel(string text, int count, int yPosition, Color highlightColor)
        {
            var displayText = count > 1 ? $"{text} ({count})" : text;

            var label = new Label
            {
                Text = displayText,
                ForeColor = AppConfiguration.OverlayTextColor,
                Font = _listFont,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(0, yPosition)
            };

            // Add a small colored indicator
            var indicator = new Panel
            {
                Size = new Size(3, 14),
                BackColor = highlightColor,
                Location = new Point(_explorationNotableBodiesPanel.Width - 10, yPosition + 2)
            };

            _explorationNotableBodiesPanel.Controls.Add(label);
            _explorationNotableBodiesPanel.Controls.Add(indicator);
        }
    }
}
