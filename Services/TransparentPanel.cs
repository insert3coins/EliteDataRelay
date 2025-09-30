using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    /// <summary>
    /// A custom panel that supports true transparency and is double-buffered to prevent flicker.
    /// Ideal for use as a drawing canvas on top of other controls.
    /// </summary>
    public class TransparentPanel : Panel
    {
        public TransparentPanel()
        {
            // Enable double-buffering to reduce flicker during animation.
            this.DoubleBuffered = true;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00000020; // WS_EX_TRANSPARENT
                return cp;
            }
        }
    }
}