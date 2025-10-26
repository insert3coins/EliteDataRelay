using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    /// <summary>
    /// A Panel with double buffering enabled to prevent flicker during custom painting.
    /// </summary>
    public class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            // Enable double buffering to prevent flicker
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.UserPaint |
                         ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.OptimizedDoubleBuffer, true);
            this.UpdateStyles();
        }
    }
}
