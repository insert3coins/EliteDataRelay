using System.Drawing;
using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    public class CustomTabControl : TabControl
    {
        private readonly Color _backColor = Color.FromArgb(18, 18, 22);
        private readonly Color _selectedTabColor = Color.FromArgb(28, 28, 35);
        private readonly Color _textColor = Color.FromArgb(243, 244, 246);
        private readonly Color _borderColor = Color.FromArgb(99, 102, 241);

        public CustomTabControl()
        {
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.SizeMode = TabSizeMode.Fixed;
            this.ItemSize = new Size(150, 40);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Clear the background
            using (var backBrush = new SolidBrush(_backColor))
            {
                e.Graphics.FillRectangle(backBrush, this.ClientRectangle);
            }

            // Draw the border for the tab page area
            if (this.SelectedTab != null)
            {
                var pageRect = this.SelectedTab.Bounds;
                var borderRect = new Rectangle(pageRect.Left - 2, pageRect.Top - 2, pageRect.Width + 4, pageRect.Height + 4);
                ControlPaint.DrawBorder(e.Graphics, borderRect, _borderColor, ButtonBorderStyle.Solid);
            }

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
                                      TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                // Border for selected tab
                if (isSelected)
                {
                    ControlPaint.DrawBorder(e.Graphics, tabRect, _borderColor, ButtonBorderStyle.Solid);
                }
            }
        }
    }
}