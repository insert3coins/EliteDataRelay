using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    /// <summary>
    /// Manages the "watching" animation on a UI control.
    /// </summary>
    public class WatchingAnimationManager : IDisposable
    {
        private readonly Button _animationLabel;
        private readonly System.Windows.Forms.Timer _animationTimer;
        private int _animationFrame = 0;

        private static readonly string[] WatchingCargoFrames = new[]
        {
            "⢄", "⢂", "⢁", " ", "⡈", "⡐", "⡠", "⡰", "⣠", "⣐", "⣈", "⣁", "⣂", "⣄", "⣆", "⣇", "⣧", "⣷", "⣾", "⣶", "⣼", "⣸", "⣙", "⣉", "⣁"
        };

        public WatchingAnimationManager(Button animationLabel)
        {
            _animationLabel = animationLabel ?? throw new ArgumentNullException(nameof(animationLabel));
            _animationTimer = new System.Windows.Forms.Timer { Interval = 100 };
            _animationTimer.Tick += AnimationTimer_Tick;
        }

        public void Start()
        {
            _animationFrame = 0;
            _animationLabel.Text = WatchingCargoFrames[_animationFrame];
            _animationLabel.ForeColor = Color.Black; // Use a distinct color for visibility
            _animationTimer.Start();
        }

        public void Stop()
        {
            _animationTimer.Stop();
            _animationLabel.Text = "";
            _animationLabel.ForeColor = SystemColors.ControlText; // Reset to default color
        }

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            _animationFrame = (_animationFrame + 1) % WatchingCargoFrames.Length;
            _animationLabel.Text = WatchingCargoFrames[_animationFrame];
        }

        public static int CalculateMaxWidth(Font animationFont)
        {
            return animationFont == null ? 20 : WatchingCargoFrames.Max(frame => TextRenderer.MeasureText(frame, animationFont).Width);
        }

        public void Dispose()
        {
            _animationTimer?.Dispose();
        }
    }
}