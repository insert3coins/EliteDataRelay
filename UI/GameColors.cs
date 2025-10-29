using System.Drawing;
using System.Drawing.Drawing2D;
using EliteDataRelay.Configuration;

namespace EliteDataRelay.UI
{
    /// <summary>
    /// Elite Dangerous game-themed color palette and graphics resources.
    /// Inspired by SrvSurvey's GameColors system.
    /// Colors are configurable through AppConfiguration.
    /// </summary>
    public static class GameColors
    {
        // Primary colors - use configuration where available
        public static Color Orange => AppConfiguration.OverlayTextColor;
        public static readonly Color OrangeDim = Color.FromArgb(95, 48, 3);
        public static readonly Color Cyan = Color.FromArgb(84, 223, 237);
        public static readonly Color CyanDim = Color.FromArgb(42, 111, 118);

        // Status colors
        public static readonly Color Gold = Color.FromArgb(255, 215, 0);
        public static readonly Color Green = Color.FromArgb(34, 139, 34);
        public static readonly Color GrayText = Color.FromArgb(160, 160, 160);
        public static readonly Color White = Color.FromArgb(220, 220, 220);

        // Background colors - use configuration
        public static Color BackgroundDark => Color.FromArgb(220,
            AppConfiguration.OverlayBackgroundColor.R,
            AppConfiguration.OverlayBackgroundColor.G,
            AppConfiguration.OverlayBackgroundColor.B);
        public static readonly Color BackgroundMedium = Color.FromArgb(180, 20, 20, 20);

        // Border color - use configuration
        public static Color BorderColor => AppConfiguration.OverlayBorderColor;

        // Pens for drawing (recreated when colors change)
        public static Pen PenBorder2 => new Pen(BorderColor, 2f);
        public static Pen PenBorder1 => new Pen(BorderColor, 1f);
        public static Pen PenOrange2 => new Pen(Orange, 2f);
        public static Pen PenOrange1 => new Pen(Orange, 1f);
        public static Pen PenCyan2 => new Pen(Cyan, 2f);
        public static Pen PenGrayDim1 => new Pen(GrayText, 1f);

        // Brushes for filling (recreated when colors change)
        public static SolidBrush BrushOrange => new SolidBrush(Orange);
        public static SolidBrush BrushCyan => new SolidBrush(Cyan);
        public static SolidBrush BrushGold => new SolidBrush(Gold);
        public static SolidBrush BrushGreen => new SolidBrush(Green);
        public static SolidBrush BrushGrayText => new SolidBrush(GrayText);
        public static SolidBrush BrushWhite => new SolidBrush(White);
        public static SolidBrush BrushBackgroundDark => new SolidBrush(BackgroundDark);

        // Fonts - use configuration font settings with increased sizes
        private static Font? _fontHeader;
        private static Font? _fontNormal;
        private static Font? _fontSmall;

        public static Font FontHeader
        {
            get
            {
                if (_fontHeader == null || _fontHeader.Size != AppConfiguration.OverlayFontSize + 2f)
                {
                    _fontHeader?.Dispose();
                    _fontHeader = new Font(AppConfiguration.OverlayFontName, AppConfiguration.OverlayFontSize + 2f, FontStyle.Bold);
                }
                return _fontHeader;
            }
        }

        public static Font FontNormal
        {
            get
            {
                if (_fontNormal == null || _fontNormal.Size != AppConfiguration.OverlayFontSize)
                {
                    _fontNormal?.Dispose();
                    _fontNormal = new Font(AppConfiguration.OverlayFontName, AppConfiguration.OverlayFontSize, FontStyle.Regular);
                }
                return _fontNormal;
            }
        }

        public static Font FontSmall
        {
            get
            {
                if (_fontSmall == null || _fontSmall.Size != AppConfiguration.OverlayFontSize - 2f)
                {
                    _fontSmall?.Dispose();
                    _fontSmall = new Font(AppConfiguration.OverlayFontName, AppConfiguration.OverlayFontSize - 2f, FontStyle.Regular);
                }
                return _fontSmall;
            }
        }

        /// <summary>
        /// Configures Graphics object for high-quality rendering.
        /// </summary>
        public static void ConfigureHighQuality(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        }
    }
}




