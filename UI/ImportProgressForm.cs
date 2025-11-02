using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using EliteDataRelay.Models;

namespace EliteDataRelay.UI
{
    public class ImportProgressForm : Form
    {
        private readonly Label _titleLabel;
        private readonly Label _fileLabel;
        private readonly ProgressBar _overallProgress;

        public ImportProgressForm()
        {
            Text = "Importing Exploration History";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            TopMost = false;
            ClientSize = new Size(480, 130);

            _titleLabel = new Label
            {
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Top,
                Height = 28,
                Padding = new Padding(10, 8, 10, 0),
                Text = "Scanning journals..."
            };

            _overallProgress = new ProgressBar
            {
                Dock = DockStyle.Top,
                Height = 24,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Style = ProgressBarStyle.Continuous,
                Padding = new Padding(10)
            };

            _fileLabel = new Label
            {
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 8, 10, 10),
                Text = string.Empty
            };

            Controls.Add(_fileLabel);
            Controls.Add(_overallProgress);
            Controls.Add(_titleLabel);
        }

        public void UpdateProgress(ImportProgress progress)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => UpdateProgress(progress)));
                return;
            }

            _titleLabel.Text = progress.Message ?? "Scanning journals...";

            var fileName = string.IsNullOrEmpty(progress.CurrentFileName)
                ? string.Empty
                : Path.GetFileName(progress.CurrentFileName);

            _fileLabel.Text = progress.TotalFiles > 0
                ? $"File {progress.CurrentFileIndex}/{progress.TotalFiles}: {fileName} ({progress.CurrentFilePercent}%) — Overall {progress.OverallPercent}%"
                : $"{fileName} ({progress.CurrentFilePercent}%)";

            var overall = Math.Max(_overallProgress.Minimum, Math.Min(_overallProgress.Maximum, progress.OverallPercent));
            _overallProgress.Value = overall;
        }
    }
}

