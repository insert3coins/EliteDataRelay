using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace EliteDataRelay.UI
{
    /// <summary>
    /// Generates icons for different celestial body types.
    /// </summary>
    public static class BodyIconGenerator
    {
        private const int IconSize = 20;

        /// <summary>
        /// Gets an icon for a specific body type.
        /// </summary>
        public static Image GetIconForBodyType(string bodyType)
        {
            if (string.IsNullOrEmpty(bodyType))
                return CreateDefaultIcon();

            // Normalize the body type
            var normalizedType = bodyType.ToLowerInvariant().Trim();

            // Stars
            if (normalizedType.Contains("star") || normalizedType.StartsWith("o ") || normalizedType.StartsWith("b ") ||
                normalizedType.StartsWith("a ") || normalizedType.StartsWith("f ") || normalizedType.StartsWith("g ") ||
                normalizedType.StartsWith("k ") || normalizedType.StartsWith("m ") || normalizedType.StartsWith("l ") ||
                normalizedType.StartsWith("t ") || normalizedType.StartsWith("y "))
            {
                return CreateStarIcon(normalizedType);
            }

            // Gas Giants
            if (normalizedType.Contains("gas giant") || normalizedType.Contains("class i") || normalizedType.Contains("class ii") ||
                normalizedType.Contains("class iii") || normalizedType.Contains("class iv") || normalizedType.Contains("class v"))
            {
                return CreateGasGiantIcon();
            }

            // Earth-like worlds
            if (normalizedType.Contains("earth") || normalizedType.Contains("earthlike"))
            {
                return CreateEarthLikeIcon();
            }

            // Water worlds
            if (normalizedType.Contains("water"))
            {
                return CreateWaterWorldIcon();
            }

            // Ammonia worlds
            if (normalizedType.Contains("ammonia"))
            {
                return CreateAmmoniaWorldIcon();
            }

            // High metal content
            if (normalizedType.Contains("high metal"))
            {
                return CreateHighMetalIcon();
            }

            // Metal-rich
            if (normalizedType.Contains("metal") && normalizedType.Contains("rich"))
            {
                return CreateMetalRichIcon();
            }

            // Rocky body / Rocky ice
            if (normalizedType.Contains("rocky"))
            {
                if (normalizedType.Contains("ice"))
                    return CreateRockyIceIcon();
                return CreateRockyIcon();
            }

            // Icy body
            if (normalizedType.Contains("icy"))
            {
                return CreateIcyIcon();
            }

            return CreateDefaultIcon();
        }

        private static Image CreateStarIcon(string starType)
        {
            var bitmap = new Bitmap(IconSize, IconSize);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // Star color based on type
                Color starColor;
                if (starType.Contains("o ")) starColor = Color.FromArgb(155, 176, 255); // Blue
                else if (starType.Contains("b ")) starColor = Color.FromArgb(170, 191, 255); // Blue-white
                else if (starType.Contains("a ")) starColor = Color.FromArgb(202, 215, 255); // White
                else if (starType.Contains("f ")) starColor = Color.FromArgb(248, 247, 255); // Yellow-white
                else if (starType.Contains("g ")) starColor = Color.FromArgb(255, 244, 234); // Yellow (like our sun)
                else if (starType.Contains("k ")) starColor = Color.FromArgb(255, 210, 161); // Orange
                else if (starType.Contains("m ")) starColor = Color.FromArgb(255, 204, 111); // Red
                else if (starType.Contains("l ") || starType.Contains("t ")) starColor = Color.FromArgb(139, 69, 19); // Brown dwarf
                else starColor = Color.FromArgb(255, 255, 200); // Default yellow

                // Draw star with glow
                using (var path = new GraphicsPath())
                {
                    // Create star shape (simple circle)
                    path.AddEllipse(2, 2, IconSize - 4, IconSize - 4);

                    using (var brush = new SolidBrush(starColor))
                    {
                        g.FillPath(brush, path);
                    }

                    // Add glow effect
                    using (var pen = new Pen(Color.FromArgb(80, starColor), 2))
                    {
                        g.DrawEllipse(pen, 1, 1, IconSize - 2, IconSize - 2);
                    }
                }
            }
            return bitmap;
        }

        private static Image CreateGasGiantIcon()
        {
            var bitmap = new Bitmap(IconSize, IconSize);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // Draw planet circle
                using (var brush = new LinearGradientBrush(
                    new Rectangle(0, 0, IconSize, IconSize),
                    Color.FromArgb(218, 165, 122), // Sandy brown
                    Color.FromArgb(244, 164, 96),   // Sandy
                    LinearGradientMode.Vertical))
                {
                    g.FillEllipse(brush, 2, 2, IconSize - 4, IconSize - 4);
                }

                // Draw horizontal bands
                using (var pen = new Pen(Color.FromArgb(100, 139, 101, 77), 1))
                {
                    g.DrawLine(pen, 4, IconSize / 2 - 2, IconSize - 4, IconSize / 2 - 2);
                    g.DrawLine(pen, 3, IconSize / 2 + 2, IconSize - 3, IconSize / 2 + 2);
                }
            }
            return bitmap;
        }

        private static Image CreateEarthLikeIcon()
        {
            var bitmap = new Bitmap(IconSize, IconSize);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // Draw blue ocean
                using (var brush = new SolidBrush(Color.FromArgb(65, 105, 225))) // Royal blue
                {
                    g.FillEllipse(brush, 2, 2, IconSize - 4, IconSize - 4);
                }

                // Draw green continents (simple shapes)
                using (var brush = new SolidBrush(Color.FromArgb(34, 139, 34))) // Forest green
                {
                    g.FillEllipse(brush, 5, 5, 6, 5);
                    g.FillEllipse(brush, 10, 8, 5, 6);
                }

                // White clouds
                using (var brush = new SolidBrush(Color.FromArgb(120, 255, 255, 255)))
                {
                    g.FillEllipse(brush, 3, 3, 4, 3);
                    g.FillEllipse(brush, 12, 11, 5, 3);
                }
            }
            return bitmap;
        }

        private static Image CreateWaterWorldIcon()
        {
            var bitmap = new Bitmap(IconSize, IconSize);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // Deep blue water
                using (var brush = new SolidBrush(Color.FromArgb(0, 119, 190)))
                {
                    g.FillEllipse(brush, 2, 2, IconSize - 4, IconSize - 4);
                }

                // Wave patterns
                using (var pen = new Pen(Color.FromArgb(100, 135, 206, 235), 1))
                {
                    g.DrawArc(pen, 4, 6, 8, 6, 0, 180);
                    g.DrawArc(pen, 8, 10, 6, 4, 0, 180);
                }
            }
            return bitmap;
        }

        private static Image CreateAmmoniaWorldIcon()
        {
            var bitmap = new Bitmap(IconSize, IconSize);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // Purple/violet ammonia atmosphere
                using (var brush = new SolidBrush(Color.FromArgb(138, 43, 226))) // Blue violet
                {
                    g.FillEllipse(brush, 2, 2, IconSize - 4, IconSize - 4);
                }

                // Toxic swirls
                using (var pen = new Pen(Color.FromArgb(100, 186, 85, 211), 1))
                {
                    g.DrawEllipse(pen, 6, 6, 8, 8);
                }
            }
            return bitmap;
        }

        private static Image CreateHighMetalIcon()
        {
            var bitmap = new Bitmap(IconSize, IconSize);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // Metallic brown/red
                using (var brush = new SolidBrush(Color.FromArgb(165, 42, 42))) // Brown
                {
                    g.FillEllipse(brush, 2, 2, IconSize - 4, IconSize - 4);
                }

                // Metallic sheen
                using (var brush = new SolidBrush(Color.FromArgb(80, 255, 255, 255)))
                {
                    g.FillEllipse(brush, 5, 4, 6, 5);
                }
            }
            return bitmap;
        }

        private static Image CreateMetalRichIcon()
        {
            var bitmap = new Bitmap(IconSize, IconSize);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // Dark metallic
                using (var brush = new SolidBrush(Color.FromArgb(105, 105, 105))) // Dim gray
                {
                    g.FillEllipse(brush, 2, 2, IconSize - 4, IconSize - 4);
                }

                // Bright metallic highlight
                using (var brush = new SolidBrush(Color.FromArgb(120, 192, 192, 192)))
                {
                    g.FillEllipse(brush, 6, 5, 5, 4);
                }
            }
            return bitmap;
        }

        private static Image CreateRockyIcon()
        {
            var bitmap = new Bitmap(IconSize, IconSize);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // Gray rocky surface
                using (var brush = new SolidBrush(Color.FromArgb(128, 128, 128)))
                {
                    g.FillEllipse(brush, 2, 2, IconSize - 4, IconSize - 4);
                }

                // Craters
                using (var pen = new Pen(Color.FromArgb(80, 0, 0, 0), 1))
                {
                    g.DrawEllipse(pen, 5, 6, 3, 3);
                    g.DrawEllipse(pen, 11, 8, 2, 2);
                }
            }
            return bitmap;
        }

        private static Image CreateRockyIceIcon()
        {
            var bitmap = new Bitmap(IconSize, IconSize);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // Light blue-gray
                using (var brush = new SolidBrush(Color.FromArgb(176, 196, 222))) // Light steel blue
                {
                    g.FillEllipse(brush, 2, 2, IconSize - 4, IconSize - 4);
                }

                // Ice patches (white)
                using (var brush = new SolidBrush(Color.FromArgb(150, 255, 255, 255)))
                {
                    g.FillEllipse(brush, 6, 5, 4, 3);
                    g.FillEllipse(brush, 11, 9, 3, 3);
                }
            }
            return bitmap;
        }

        private static Image CreateIcyIcon()
        {
            var bitmap = new Bitmap(IconSize, IconSize);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // White/light cyan ice
                using (var brush = new SolidBrush(Color.FromArgb(224, 255, 255))) // Light cyan
                {
                    g.FillEllipse(brush, 2, 2, IconSize - 4, IconSize - 4);
                }

                // Ice crystals (sparkles)
                using (var pen = new Pen(Color.FromArgb(150, 255, 255, 255), 1))
                {
                    g.DrawLine(pen, 7, 7, 9, 9);
                    g.DrawLine(pen, 9, 7, 7, 9);
                    g.DrawLine(pen, 12, 11, 14, 13);
                }
            }
            return bitmap;
        }

        private static Image CreateDefaultIcon()
        {
            var bitmap = new Bitmap(IconSize, IconSize);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // Generic gray planet
                using (var brush = new SolidBrush(Color.FromArgb(169, 169, 169)))
                {
                    g.FillEllipse(brush, 2, 2, IconSize - 4, IconSize - 4);
                }
            }
            return bitmap;
        }
    }
}
