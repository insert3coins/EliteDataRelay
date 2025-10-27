using System.Drawing;
using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    public class CustomTabControl : TabControl
    {
        private readonly Color _backColor = Color.White;                      // Control background
        private readonly Color _selectedTabColor = Color.FromArgb(243, 244, 246); // Selected tab bg
        private readonly Color _textColor = Color.FromArgb(17, 24, 39);          // #111827
        private readonly Color _borderColor = Color.FromArgb(209, 213, 219);     // #D1D5DB

        public CustomTabControl()
        {
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.SizeMode = TabSizeMode.Fixed;
            this.ItemSize = new Size(140, 36);
            this.Multiline = true; // Wrap into multiple rows instead of showing scroll arrows
            this.Padding = new Point(12, 6);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Clear the background
            using (var backBrush = new SolidBrush(_backColor))
            {
                e.Graphics.FillRectangle(backBrush, this.ClientRectangle);
            }

            // Subtle bottom border under the tab strip
            var tabsArea = new Rectangle(0, 0, this.Width, this.GetTabRect(0).Bottom + 1);
            ControlPaint.DrawBorder(e.Graphics, tabsArea, _borderColor, ButtonBorderStyle.Solid);

            // Draw the tabs
            for (int i = 0; i < this.TabCount; i++)
            {
                Rectangle tabRect = this.GetTabRect(i);
                bool isSelected = (this.SelectedIndex == i);

                // Tab background
                using (var tabBrush = new SolidBrush(isSelected ? _selectedTabColor : _backColor))
                {
                    e.Graphics.FillRectangle(tabBrush, tabRect);
                }

                // Tab text
                TextRenderer.DrawText(e.Graphics, this.TabPages[i].Text, this.Font, tabRect, _textColor,
                                      TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

                // Border for selected tab
                if (isSelected)
                {
                    ControlPaint.DrawBorder(e.Graphics, tabRect, _borderColor, ButtonBorderStyle.Solid);
                }
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (this.TabCount <= 1) return;

            int direction = e.Delta > 0 ? -1 : 1; // Up = previous, Down = next
            int newIndex = this.SelectedIndex + direction;
            if (newIndex < 0) newIndex = this.TabCount - 1;
            if (newIndex >= this.TabCount) newIndex = 0;
            this.SelectedIndex = newIndex;
        }
    }
}
