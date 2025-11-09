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
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Image> _cache = new();

        private static void DrawShadedSphere(Graphics g, Rectangle rect, Color baseColor, float lightDx, float lightDy, Color? rimColor = null, bool addSpecular = true)
        {
            using (var path = new GraphicsPath())
            {
                path.AddEllipse(rect);

                using (var pgb = new PathGradientBrush(path))
                {
                    var centerX = rect.X + rect.Width * (0.5f + lightDx * 0.25f);
                    var centerY = rect.Y + rect.Height * (0.5f + lightDy * 0.25f);
                    pgb.CenterPoint = new PointF(centerX, centerY);
                    pgb.CenterColor = Lighten(baseColor, 0.35f);
                    pgb.SurroundColors = new[] { Darken(baseColor, 0.35f) };
                    g.FillPath(pgb, path);
                }

                if (addSpecular)
                {
                    // Small specular highlight towards light direction
                    var specW = Math.Max(2, rect.Width / 6);
                    var specH = Math.Max(2, rect.Height / 6);
                    var specX = rect.X + rect.Width * (0.5f + lightDx * 0.25f) - specW / 2f;
                    var specY = rect.Y + rect.Height * (0.5f + lightDy * 0.25f) - specH / 2f;
                    using (var specBrush = new SolidBrush(Color.FromArgb(120, 255, 255, 255)))
                    {
                        g.FillEllipse(specBrush, specX, specY, specW, specH);
                    }
                }

                if (rimColor.HasValue)
                {
                    using (var pen = new Pen(Color.FromArgb(100, rimColor.Value), 1))
                    {
                        g.DrawEllipse(pen, rect.X + 0.5f, rect.Y + 0.5f, rect.Width - 1, rect.Height - 1);
                    }
                }
            }
        }

        private static Color Lighten(Color c, float amount)
        {
            amount = Math.Clamp(amount, 0f, 1f);
            int r = c.R + (int)((255 - c.R) * amount);
            int g = c.G + (int)((255 - c.G) * amount);
            int b = c.B + (int)((255 - c.B) * amount);
            return Color.FromArgb(c.A, r, g, b);
        }

        private static Color Darken(Color c, float amount)
        {
            amount = Math.Clamp(amount, 0f, 1f);
            int r = c.R - (int)(c.R * amount);
            int g = c.G - (int)(c.G * amount);
            int b = c.B - (int)(c.B * amount);
            return Color.FromArgb(c.A, Math.Max(0, r), Math.Max(0, g), Math.Max(0, b));
        }

        /// <summary>
        /// Gets an icon for a specific body type.
        /// </summary>
        public static Image GetIconForBodyType(string bodyType)
        {
            if (string.IsNullOrEmpty(bodyType))
                return GetOrCreateCached("__default__", CreateDefaultIcon);

            // Normalize the body type
            var normalizedType = bodyType.ToLowerInvariant().Trim();

            if (_cache.TryGetValue(normalizedType, out var cached))
                return cached;

            // Stars
            if (normalizedType.Contains("star") || normalizedType.StartsWith("o ") || normalizedType.StartsWith("b ") ||
                normalizedType.StartsWith("a ") || normalizedType.StartsWith("f ") || normalizedType.StartsWith("g ") ||
                normalizedType.StartsWith("k ") || normalizedType.StartsWith("m ") || normalizedType.StartsWith("l ") ||
                normalizedType.StartsWith("t ") || normalizedType.StartsWith("y "))
            {
                return GetOrCreateCached(normalizedType, () => CreateStarIcon(normalizedType));
            }

            // Gas Giants
            if (normalizedType.Contains("gas giant") || normalizedType.Contains("class i") || normalizedType.Contains("class ii") ||
                normalizedType.Contains("class iii") || normalizedType.Contains("class iv") || normalizedType.Contains("class v"))
            {
                return GetOrCreateCached(normalizedType, CreateGasGiantIcon);
            }

            // Earth-like worlds
            if (normalizedType.Contains("earth") || normalizedType.Contains("earthlike"))
            {
                return GetOrCreateCached(normalizedType, CreateEarthLikeIcon);
            }

            // Water worlds
            if (normalizedType.Contains("water"))
            {
                return GetOrCreateCached(normalizedType, CreateWaterWorldIcon);
            }

            // Ammonia worlds
            if (normalizedType.Contains("ammonia"))
            {
                return GetOrCreateCached(normalizedType, CreateAmmoniaWorldIcon);
            }

            // High metal content
            if (normalizedType.Contains("high metal"))
            {
                return GetOrCreateCached(normalizedType, CreateHighMetalIcon);
            }

            // Metal-rich
            if (normalizedType.Contains("metal") && normalizedType.Contains("rich"))
            {
                return GetOrCreateCached(normalizedType, CreateMetalRichIcon);
            }

            // Rocky body / Rocky ice
            if (normalizedType.Contains("rocky"))
            {
                if (normalizedType.Contains("ice"))
                    return GetOrCreateCached(normalizedType, CreateRockyIceIcon);
                return GetOrCreateCached(normalizedType, CreateRockyIcon);
            }

            // Icy body
            if (normalizedType.Contains("icy"))
            {
                return GetOrCreateCached(normalizedType, CreateIcyIcon);
            }

            return GetOrCreateCached(normalizedType, CreateDefaultIcon);
        }

        private static Image GetOrCreateCached(string key, Func<Image> factory)
        {
            return _cache.GetOrAdd(key, _ => factory());
        }

        public static void ClearCache()
        {
            foreach (var kvp in _cache)
            {
                try { kvp.Value.Dispose(); } catch { }
            }
            _cache.Clear();
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

                // Core glow using radial gradient
                var sphereRect = new Rectangle(2, 2, IconSize - 4, IconSize - 4);
                using (var path = new GraphicsPath())
                {
                    path.AddEllipse(sphereRect);
                    using (var pgb = new PathGradientBrush(path))
                    {
                        pgb.CenterPoint = new PointF(sphereRect.X + sphereRect.Width / 2f, sphereRect.Y + sphereRect.Height / 2f);
                        pgb.CenterColor = Lighten(starColor, 0.45f);
                        pgb.SurroundColors = new[] { Darken(starColor, 0.25f) };
                        g.FillPath(pgb, path);
                    }
                }

                // Soft outer halo
                using (var haloPath = new GraphicsPath())
                {
                    haloPath.AddEllipse(0.5f, 0.5f, IconSize - 1f, IconSize - 1f);
                    using (var haloPen = new Pen(Color.FromArgb(90, starColor), 2))
                    {
                        g.DrawPath(haloPen, haloPath);
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
                var base1 = Color.FromArgb(218, 165, 122);
                DrawShadedSphere(g, new Rectangle(2, 2, IconSize - 4, IconSize - 4), base1, -0.6f, -0.4f, null, addSpecular: false);

                // Curved bands
                var bandColors = new[]
                {
                    Color.FromArgb(110, 139, 101, 77),
                    Color.FromArgb(110, 205, 133, 63),
                    Color.FromArgb(110, 222, 184, 135)
                };
                for (int i = -2; i <= 2; i++)
                {
                    var h = Math.Max(1, 2 - Math.Abs(i));
                    var color = bandColors[(i + bandColors.Length * 10) % bandColors.Length];
                    using (var pen = new Pen(color, h))
                    {
                        g.DrawArc(pen, 3, 5, IconSize - 6, IconSize - 10, 0, 180);
                    }
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
                var ocean = Color.FromArgb(65, 105, 225);
                DrawShadedSphere(g, new Rectangle(2, 2, IconSize - 4, IconSize - 4), ocean, -0.6f, -0.5f, Color.FromArgb(135, 173, 255));

                // Continents (slightly translucent and irregular)
                using (var land = new SolidBrush(Color.FromArgb(180, 34, 139, 34)))
                {
                    g.FillEllipse(land, 4, 6, 7, 5);
                    g.FillEllipse(land, 10, 8, 4, 6);
                }

                // Cloud streaks
                using (var cloud = new SolidBrush(Color.FromArgb(110, 255, 255, 255)))
                {
                    g.FillEllipse(cloud, 5, 4, 6, 3);
                    g.FillEllipse(cloud, 9, 11, 6, 3);
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
                var deep = Color.FromArgb(0, 119, 190);
                DrawShadedSphere(g, new Rectangle(2, 2, IconSize - 4, IconSize - 4), deep, -0.6f, -0.5f, Color.FromArgb(120, 180, 230), addSpecular: true);

                using (var pen = new Pen(Color.FromArgb(90, 200, 230, 255), 1))
                {
                    g.DrawArc(pen, 5, 7, 8, 5, 0, 180);
                    g.DrawArc(pen, 7, 10, 6, 4, 0, 180);
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
                var atm = Color.FromArgb(138, 43, 226);
                DrawShadedSphere(g, new Rectangle(2, 2, IconSize - 4, IconSize - 4), atm, -0.6f, -0.4f, Color.FromArgb(170, 120, 230));

                using (var pen = new Pen(Color.FromArgb(100, 186, 85, 211), 1))
                {
                    g.DrawArc(pen, 4, 6, 10, 8, 20, 140);
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
                var metal = Color.FromArgb(165, 42, 42);
                DrawShadedSphere(g, new Rectangle(2, 2, IconSize - 4, IconSize - 4), metal, -0.6f, -0.5f, null, addSpecular: true);
            }
            return bitmap;
        }

        private static Image CreateMetalRichIcon()
        {
            var bitmap = new Bitmap(IconSize, IconSize);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var darkMetal = Color.FromArgb(105, 105, 105);
                DrawShadedSphere(g, new Rectangle(2, 2, IconSize - 4, IconSize - 4), darkMetal, -0.6f, -0.5f, null, addSpecular: true);
            }
            return bitmap;
        }

        private static Image CreateRockyIcon()
        {
            var bitmap = new Bitmap(IconSize, IconSize);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var surface = Color.FromArgb(128, 128, 128);
                DrawShadedSphere(g, new Rectangle(2, 2, IconSize - 4, IconSize - 4), surface, -0.6f, -0.5f, null, addSpecular: false);

                using (var shadow = new Pen(Color.FromArgb(100, 0, 0, 0), 1))
                using (var light = new Pen(Color.FromArgb(120, 220, 220, 220), 1))
                {
                    g.DrawArc(shadow, 5, 6, 3, 3, 200, 160);
                    g.DrawArc(light, 5, 6, 3, 3, 20, 160);
                    g.DrawArc(shadow, 10, 8, 2, 2, 200, 160);
                    g.DrawArc(light, 10, 8, 2, 2, 20, 160);
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
                var iceRock = Color.FromArgb(176, 196, 222);
                DrawShadedSphere(g, new Rectangle(2, 2, IconSize - 4, IconSize - 4), iceRock, -0.6f, -0.5f, null, addSpecular: true);

                using (var patch = new SolidBrush(Color.FromArgb(130, 255, 255, 255)))
                {
                    g.FillEllipse(patch, 6, 5, 4, 3);
                    g.FillEllipse(patch, 11, 9, 3, 3);
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
                var ice = Color.FromArgb(224, 255, 255);
                DrawShadedSphere(g, new Rectangle(2, 2, IconSize - 4, IconSize - 4), ice, -0.6f, -0.5f, Color.FromArgb(200, 235, 255), addSpecular: true);

                using (var pen = new Pen(Color.FromArgb(140, 255, 255, 255), 1))
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
                var generic = Color.FromArgb(169, 169, 169);
                DrawShadedSphere(g, new Rectangle(2, 2, IconSize - 4, IconSize - 4), generic, -0.5f, -0.5f, null, addSpecular: false);
            }
            return bitmap;
        }
    }
}
