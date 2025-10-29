using System;
using System.Drawing;
using System.Windows.Forms;

namespace EliteDataRelay.UI

{
    public class WatchingAnimationManager : IDisposable
    {
        private static readonly string[] WatchingCargoFrames = new[]
        {
            "⢄", "⢂", "⢁", " ", "⡈", "⡐", "⡠", "⡰", "⣠", "⣐", "⣈", "⣁", "⣂", "⣄", "⣆", "⣇", "⣧", "⣷", "⣾", "⣶", "⣼", "⣸", "⣙", "⣉", "⣁"
        };

        private readonly System.Windows.Forms.Timer _animationTimer;
        private readonly Label _label;
        private int _frameCount;
        private bool _isMonitoringActive;

        public WatchingAnimationManager(Label label)
        {
            _label = label;
            _animationTimer = new System.Windows.Forms.Timer { Interval = 50 }; // Faster interval for smooth animation
            _animationTimer.Tick += OnAnimationTick;
        }

        public void SetMonitoringState(bool isActive)
        {
            _isMonitoringActive = isActive;
            if (isActive)
            {
                Start();
            }
            else
            {
                Stop();
                _label.Text = "";
            }
        }

        public void Start() => _animationTimer.Start();

        public void Stop() => _animationTimer.Stop();

        public void StopIfInactive()
        {
            if (!_isMonitoringActive)
            {
                Stop();
                _label.Text = "";
            }
        }

        private void OnAnimationTick(object? sender, EventArgs e)
        {
            _frameCount = (_frameCount + 1) % WatchingCargoFrames.Length;
            _label.Text = WatchingCargoFrames[_frameCount];
        }

        public static int CalculateMaxWidth(Font font)
        {
            // All frames are single characters, so we just need the width of one.
            return TextRenderer.MeasureText("W", font).Width + 5; // Add padding
        }

        public void Dispose() => _animationTimer.Dispose();
    }
}



