using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Linq;

using System.Reflection;
namespace EliteDataRelay.Services
{
    /// <summary>
    /// Provides a service to retrieve ship icons based on the ship's internal name.
    /// Icons are loaded from the file system and cached in memory.
    /// </summary>
    public static class ShipIconService
    {
        private static readonly Dictionary<string, Image> _iconCache = new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);
        //private static readonly string _shipIconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images", "ships");
        private static Image? _defaultIcon;
        private static readonly Dictionary<string, string> _embeddedResourceIndex = new(StringComparer.OrdinalIgnoreCase);

        // Display names and common aliases -> file base name
        // This lets callers pass user-facing names (e.g., "Cobra MkIII", "Type-6 Transporter", "Fer-de-Lance")
        // and we resolve them to the actual filename we ship with.
        private static readonly Dictionary<string, string> _aliasToFileNameMap = new(StringComparer.OrdinalIgnoreCase)
        {
            // Core names (as displayed) mapped to our filenames
            { "Adder", "Adder" },
            { "Alliance Challenger", "Alliance Challenger" },
            { "Alliance Chieftain", "Alliance Chieftain" },
            { "Alliance Crusader", "Alliance Crusader" },
            { "Anaconda", "Anaconda" },
            { "Asp Explorer", "Asp Explorer" },
            { "Asp Scout", "Asp Scout" },
            { "Beluga Liner", "Beluga Liner" },
            { "Cobra Mk III", "Cobra Mk III" },
            { "Cobra Mk IV", "Cobra Mk IV" },
            { "Cobra Mk V", "Cobra Mk V" },
            { "Corsair", "Corsair" },
            { "Cyclops", "Cyclops" },
            { "Diamondback Explorer", "DiamondBack Explorer" },
            { "Diamondback Scout", "Diamondback Scout" },
            { "Dolphin", "Dolphin" },
            { "Eagle Mk II", "Eagle Mk II" },
            { "Federal Assault Ship", "Federal Assault Ship" },
            { "Federal Dropship", "Federal Dropship" },
            { "Federal Gunship", "Federal_Gunship" },
            { "Federal Corvette", "Federation_Corvette" },
            { "Federation Corvette", "Federation_Corvette" },
            { "Fer De Lance", "Fer De Lance" },
            { "Hauler", "Hauler" },
            { "Imperial Clipper", "Imperial Clipper" },
            { "Imperial Courier", "Imperial Courier" },
            { "Imperial Eagle", "Imperial Eagle" },
            { "Imperial Cutter", "Cutter" },
            { "Cutter", "Cutter" },
            { "Keelback", "Keelback" },
            { "Krait Mk II", "Krait Mk II" },
            { "Krait Phantom", "Krait Phantom" },
            { "Mamba", "Mamba" },
            { "Mandalay", "Mandalay" },
            { "Orca", "Orca" },
            { "Panther Clipper Mk II", "Panther Clipper Mk II" },
            { "Python Mk II", "Python Mk II" },
            { "Python", "Python" },
            { "Sidewinder", "Sidewinder" },
            { "Type 6 Transporter", "Type 6 Transporter" },
            { "Type 7 Transporter", "Type 7 Transporter" },
            { "Type 9 Heavy", "Type 9 Heavy" },
            { "Type 9 Military", "Type9_Military" },
            { "Type9 Military", "Type9_Military" },
            { "Type 10 Defender", "Type 10 Defender" },
            { "Type-10 Defender", "Type 10 Defender" },
            { "Type-11 Prospector", "Type-11 Prospector" },
            { "Type 11 Prospector", "Type-11 Prospector" },
            { "Type-8 Transporter", "Type-8 Transporter" },
            { "Viper Mk III", "Viper Mk III" },
            { "Viper Mk IV", "Viper Mk IV" },
            { "Vulture", "Vulture" },

            // Special modes (allow real PNGs to be shipped)
            { "SRV", "SRV" },
            { "Fighter", "Fighter" },
            { "OnFoot", "On Foot" },
            { "On Foot", "On Foot" },
            { "Artemis", "Artemis" },
            { "Maverick", "Maverick" },
            { "Dominator", "Dominator" },

            // Mk with no space variants
            { "Cobra MkIII", "Cobra Mk III" },
            { "Cobra MkIV", "Cobra Mk IV" },
            { "Cobra MkV", "Cobra Mk V" },
            { "Eagle MkII", "Eagle Mk II" },
            { "Krait MkII", "Krait Mk II" },
            { "Python MkII", "Python Mk II" },
            { "Viper MkIII", "Viper Mk III" },
            { "Viper MkIV", "Viper Mk IV" },
            { "Panther Clipper MkII", "Panther Clipper Mk II" },

            // Hyphen/space variants for Type series
            { "Type-6 Transporter", "Type 6 Transporter" },
            { "Type-7 Transporter", "Type 7 Transporter" },
            { "Type 8 Transporter", "Type-8 Transporter" },
            { "Type9 Heavy", "Type 9 Heavy" },

            // Fer-de-Lance punctuation variant
            { "Fer-de-Lance", "Fer De Lance" },

            // Minor spacing/casing aliases
            // Note: dictionary is case-insensitive; a single alias covers case variants

            // Redirect our internal-based display names to named files
            { "Type9_Military", "Type 10 Defender" },
            { "Lakon_Miner", "Type-11 Prospector" }
        };

        /// <summary>
        /// Static constructor to log the ship icon mappings on startup.
        /// </summary>
        static ShipIconService()
        {
            Trace.WriteLine("[ShipIconService] Initializing ship icon mappings...");
            // Index embedded PNG resources by filename (without extension)
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                var names = asm.GetManifestResourceNames();
                foreach (var res in names)
                {
                    if (!res.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var withoutExt = res.Substring(0, res.Length - 4);
                    var lastDot = withoutExt.LastIndexOf('.');
                    var baseName = lastDot >= 0 ? withoutExt.Substring(lastDot + 1) : withoutExt;

                    if (!string.IsNullOrWhiteSpace(baseName) && !_embeddedResourceIndex.ContainsKey(baseName))
                    {
                        _embeddedResourceIndex[baseName] = res;
                    }
                }
                Trace.WriteLine($"[ShipIconService] Embedded PNGs indexed: {_embeddedResourceIndex.Count}");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[ShipIconService] Failed to index embedded resources: {ex.Message}");
            }
            foreach (var mapping in _shipToFileNameMap)
            {
                Trace.WriteLine($"[ShipIconService] Mapping: '{mapping.Key}' -> '{mapping.Value}.png'");
            }
            Trace.WriteLine($"[ShipIconService] Total mappings loaded: {_shipToFileNameMap.Count}");

            Trace.WriteLine("[ShipIconService] Initializing alias/display name mappings...");
            foreach (var alias in _aliasToFileNameMap)
            {
                Trace.WriteLine($"[ShipIconService] Alias: '{alias.Key}' -> '{alias.Value}.png'");
            }
            Trace.WriteLine($"[ShipIconService] Total aliases loaded: {_aliasToFileNameMap.Count}");
        }

        // Maps the journal's internal ship name (usually lowercase) to a specific icon file name.
        // This provides a single source of truth and handles any inconsistencies in journal data.
        private static readonly Dictionary<string, string> _shipToFileNameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // --- Small Ships ---
            { "sidewinder", "SideWinder" },
            { "eagle", "Eagle Mk II" },
            { "hauler", "Hauler" },
            { "adder", "Adder" },
            { "empire_eagle", "Imperial Eagle" },
            { "viper", "Viper Mk III" },
            { "viper_mkiv", "Viper Mk IV" },
            { "cobramkiii", "Cobra Mk III" },
            { "cobramkiv", "Cobra Mk IV" },
            { "cobramkv", "Cobra Mk V" },
            { "diamondback", "Diamondback Scout" }, // Journal: "diamondback" -> Scout
            { "dolphin", "Dolphin" },
            { "empire_courier", "Imperial Courier" },
            { "vulture", "Vulture" },
            { "corsair", "Corsair" },

            // --- Medium Ships ---
            { "diamondbackxl", "Diamondback Explorer" }, // Journal: "diamondbackxl" -> Explorer
            { "keelback", "Keelback" },
            { "type6", "Type 6 Transporter" },
            { "lakonminer", "Lakon_Miner" }, // Lakon Type-11 Prospector
            { "asp", "Asp Explorer" }, // Journal: "asp" -> Asp Explorer
            { "asp_scout", "Asp Scout" },
            { "federation_dropship", "Federal Dropship" },
            { "federation_dropship_mkii", "Federal Assault Ship" },
            { "federation_gunship", "Federal_Gunship" },
            { "empire_trader", "Imperial Clipper" },
            { "krait_mkii", "Krait Mk II" },
            { "krait_phantom", "Krait Phantom" },
            { "mamba", "Mamba" },
            { "ferdelance", "Fer De Lance" },
            { "python", "Python" },
            { "python_mkii", "Python Mk II" },
            { "orca", "Orca" },
            { "typex_3" , "Alliance Challenger" },
            { "typex_2" , "Alliance Crusader" },
            { "typex", "Alliance Chieftain" }, // Alliance Chieftain
            { "type8", "Type-8 Transporter"}, // Type 8
            { "mandalay", "Mandalay" }, 

            // --- Large Ships ---
            { "type7", "Type 7 Transporter" },
            { "type9", "Type 9 Heavy" },
            { "type9_military", "Type9_Military" },
            { "belugaliner", "Beluga Liner" },
            { "anaconda", "Anaconda" },
            { "federation_corvette", "Federation_Corvette" },
            { "cutter", "Cutter" },
            { "cyclops", "Cyclops" },
            { "panthermkii" , "Panther Clipper Mk II" }
        };

        /// <summary>
        /// Gets the icon for a given ship type.
        /// </summary>
        /// <param name="internalShipName">The internal name of the ship (e.g., "CobraMkIII").</param>
        /// <returns>An <see cref="Image"/> for the ship, or null if not found.</returns>
        public static Image? GetShipIcon(string? internalShipName)
        {
            if (string.IsNullOrEmpty(internalShipName))
            {
                return GetDefaultIcon();
            }

            if (_iconCache.TryGetValue(internalShipName, out var cachedIcon))
            {
                Trace.WriteLine($"[ShipIconService] Returning cached icon for '{internalShipName}'.");
                return cachedIcon;
            }

            // Special modes: prefer an embedded PNG if present, otherwise generate a styled vector icon
            if (IsSpecialMode(internalShipName, out var modeLabel))
            {
                // Try to resolve a filename from aliases first (allows shipping real PNGs named accordingly)
                if (TryResolveAliasToFileBase(internalShipName, out var specialFile) || TryResolveAliasToFileBase(modeLabel, out specialFile))
                {
                    var embedded = TryLoadIcon(specialFile);
                    if (embedded != null)
                    {
                        _iconCache[internalShipName] = embedded;
                        return embedded;
                    }
                }

                // Fallback to generated vector icon styled like our assets
                var modeIcon = CreateModeIcon(modeLabel, internalShipName);
                _iconCache[internalShipName] = modeIcon;
                return modeIcon;
            }

            // First, try treating the input as a display name/alias.
            // This enables callers to pass user-facing names directly.
            if (TryResolveAliasToFileBase(internalShipName, out var fileName))
            {
                var icon = TryLoadIcon(fileName);
                if (icon != null)
                {
                    _iconCache[internalShipName] = icon;
                    return icon;
                }
            }

            // Otherwise, use the internal->display mapping then resolve aliases
            if (_shipToFileNameMap.TryGetValue(internalShipName, out var displayName)
                && TryResolveAliasToFileBase(displayName, out fileName))
            {
                var icon = TryLoadIcon(fileName);
                if (icon != null)
                {
                    _iconCache[internalShipName] = icon;
                    return icon;
                }
            }
            else
            {
                Trace.WriteLine($"[ShipIconService] No mapping found for ship '{internalShipName}'.");
            }

            return GetDefaultIcon();
        }

        private static bool IsSpecialMode(string name, out string label)
        {
            // Normalize
            var n = name.Trim();
            if (n.Equals("SRV", StringComparison.OrdinalIgnoreCase)) { label = "SRV"; return true; }
            if (n.Equals("Fighter", StringComparison.OrdinalIgnoreCase)) { label = "Fighter"; return true; }
            if (n.Equals("OnFoot", StringComparison.OrdinalIgnoreCase) || n.Equals("On Foot", StringComparison.OrdinalIgnoreCase)) { label = "On Foot"; return true; }
            if (n.Equals("Taxi", StringComparison.OrdinalIgnoreCase)) { label = "Taxi"; return true; }
            if (n.Equals("Multicrew", StringComparison.OrdinalIgnoreCase)) { label = "Multicrew"; return true; }
            label = string.Empty;
            return false;
        }

        /// <summary>
        /// Creates a data URL PNG for special modes (SRV, Fighter, On Foot, Taxi, Multicrew).
        /// Returns true if generated; false if not a special mode.
        /// </summary>
        public static bool TryGetSpecialModeDataUrl(string? internalShipName, out string dataUrl)
        {
            dataUrl = string.Empty;
            if (string.IsNullOrWhiteSpace(internalShipName)) return false;
            if (!IsSpecialMode(internalShipName, out var label)) return false;

            using var bmp = CreateModeIcon(label, internalShipName);
            using var ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            var base64 = Convert.ToBase64String(ms.ToArray());
            dataUrl = "data:image/png;base64," + base64;
            return true;
        }

        private static Image CreateModeIcon(string text, string? internalKey = null)
        {
            // Slightly different styling than generic placeholder
            int width = 160, height = 160;
            var bmp = new Bitmap(width, height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                g.Clear(Color.Transparent);

                var bg = Color.FromArgb(20, 20, 20);
                var border = Color.FromArgb(255, 111, 0); // Elite orange
                var accent = Color.FromArgb(255, 128, 0);

                using var bgBrush = new SolidBrush(bg);
                using var borderPen = new Pen(border, 2f);
                using var accentPen = new Pen(accent, 3f);
                using var textBrush = new SolidBrush(Color.Gainsboro);

                g.FillRectangle(bgBrush, 0, 0, width, height);
                g.DrawRectangle(borderPen, 0, 0, width - 1, height - 1);

                // Accent chevrons
                g.DrawLine(accentPen, 8, 20, width - 8, 20);
                g.DrawLine(accentPen, 8, height - 20, width - 8, height - 20);

                // Draw a simple vector glyph representing the mode
                var glyphArea = new Rectangle(20, 30, width - 40, height - 80);
                DrawModeGlyph(g, glyphArea, internalKey, text);

                // Text
                var rect = new RectangleF(10, 104, width - 20, height - 112);
                using var font = new Font("Segoe UI", 18f, FontStyle.Bold, GraphicsUnit.Pixel);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisWord };
                g.DrawString(text, font, textBrush, rect, sf);
            }
            return bmp;
        }

        private static void DrawModeGlyph(Graphics g, Rectangle area, string? internalKey, string modeLabel)
        {
            // Normalize
            string key = (internalKey ?? modeLabel ?? string.Empty).Trim();
            bool isSrv = key.Equals("SRV", StringComparison.OrdinalIgnoreCase);
            bool isFighter = key.Equals("Fighter", StringComparison.OrdinalIgnoreCase);
            bool isOnFoot = key.Equals("OnFoot", StringComparison.OrdinalIgnoreCase)
                            || key.Equals("On Foot", StringComparison.OrdinalIgnoreCase)
                            || ((modeLabel?.IndexOf("Foot", StringComparison.OrdinalIgnoreCase)) ?? -1) >= 0;
            bool isSuit = string.Equals(modeLabel, "Artemis", StringComparison.OrdinalIgnoreCase)
                          || string.Equals(modeLabel, "Maverick", StringComparison.OrdinalIgnoreCase)
                          || string.Equals(modeLabel, "Dominator", StringComparison.OrdinalIgnoreCase);

            using var orangePen = new Pen(Color.FromArgb(255,128,0), 3f) { LineJoin = System.Drawing.Drawing2D.LineJoin.Round };
            using var grayPen = new Pen(Color.FromArgb(180,180,180), 2f) { LineJoin = System.Drawing.Drawing2D.LineJoin.Round };
            using var fillBrush = new SolidBrush(Color.FromArgb(36,36,36));
            using var accentBrush = new SolidBrush(Color.FromArgb(255,128,0));

            if (isSrv)
            {
                // Simple rover: body + wheels + antenna
                var body = new Rectangle(area.X + area.Width/8, area.Y + area.Height/3, area.Width * 3/4, area.Height/3);
                g.FillRoundedRectangle(fillBrush, body, 8);
                g.DrawRoundedRectangle(orangePen, body, 8);
                int wheelR = Math.Max(6, area.Width/10);
                int wy = body.Bottom + 4;
                int wx1 = body.Left + wheelR/2;
                int wx2 = body.Left + body.Width/2 - wheelR/2;
                int wx3 = body.Right - wheelR - wheelR/2;
                g.FillEllipse(accentBrush, wx1, wy, wheelR, wheelR);
                g.FillEllipse(accentBrush, wx2, wy, wheelR, wheelR);
                g.FillEllipse(accentBrush, wx3, wy, wheelR, wheelR);
                // Antenna
                g.DrawLine(grayPen, body.Left + body.Width*3/4, body.Top, body.Left + body.Width*3/4 + 8, body.Top - 12);
                g.FillEllipse(accentBrush, body.Left + body.Width*3/4 + 8 - 2, body.Top - 12 - 2, 4, 4);
                return;
            }
            if (isFighter)
            {
                // Delta shape fighter
                var pts = new[] {
                    new Point(area.X + area.Width/2, area.Y),
                    new Point(area.X + area.Width, area.Bottom),
                    new Point(area.X, area.Bottom)
                };
                g.FillPolygon(accentBrush, pts);
                g.DrawPolygon(orangePen, pts);
                return;
            }
            if (isOnFoot || isSuit)
            {
                // Helmet silhouette: circle + visor bar
                int d = Math.Max(10, Math.Min(area.Width, area.Height) - 6);
                int cx = area.X + area.Width/2 - d/2;
                int cy = area.Y + area.Height/2 - d/2 - 6;
                var head = new Rectangle(cx, cy, d, d);
                g.FillEllipse(fillBrush, head);
                g.DrawEllipse(orangePen, head);
                // Visor
                var visor = new Rectangle(head.X + d/5, head.Y + d/2 - d/10, d*3/5, d/5);
                g.FillRoundedRectangle(accentBrush, visor, 6);
                // Collar
                var collar = new Rectangle(head.X + d/4, head.Bottom - d/6, d/2, d/6);
                g.FillRoundedRectangle(fillBrush, collar, 6);
                g.DrawRoundedRectangle(orangePen, collar, 6);
                return;
            }
            // Default: simple badge
            g.DrawEllipse(orangePen, area);
        }

        // Rounded rectangle helpers
        private static void FillRoundedRectangle(this Graphics g, Brush brush, Rectangle rect, int radius)
        {
            using var gp = RoundedRect(rect, radius);
            g.FillPath(brush, gp);
        }
        private static void DrawRoundedRectangle(this Graphics g, Pen pen, Rectangle rect, int radius)
        {
            using var gp = RoundedRect(rect, radius);
            g.DrawPath(pen, gp);
        }
        private static System.Drawing.Drawing2D.GraphicsPath RoundedRect(Rectangle rect, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            int r = Math.Max(1, radius);
            int d = r * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>
        /// Gets the display name for a given ship's internal name.
        /// </summary>
        /// <param name="internalShipName">The internal name of the ship (e.g., "CobraMkIII").</param>
        /// <returns>A user-friendly display name (e.g., "Cobra Mk III"), or the capitalized internal name as a fallback.</returns>
        public static string GetShipDisplayName(string? internalShipName)
        {
            if (string.IsNullOrEmpty(internalShipName))
            {
                return "Unknown";
            }

            // Use the mapping to find the correct file name, which is also the display name.
            return _shipToFileNameMap.TryGetValue(internalShipName, out var displayName) ? displayName : (char.ToUpperInvariant(internalShipName[0]) + internalShipName.Substring(1));
        }

        /// <summary>
        /// Gets the default "unknown" ship icon, loading it from the file system if necessary.
        /// </summary>
        /// <returns>The default ship icon, or null if it also cannot be loaded.</returns>
        private static Image? GetDefaultIcon()
        {
            if (_defaultIcon != null)
            {
                Trace.WriteLine("[ShipIconService] Returning cached default icon.");
                return _defaultIcon;
            }

            // Filesystem fallback removed; rely on embedded resources or placeholder
            _defaultIcon = CreateTextPlaceholder("No image found");
            return _defaultIcon;
        }

        private static bool TryResolveAliasToFileBase(string nameOrAlias, out string fileBase)
        {
            // Direct alias lookup
            if (_aliasToFileNameMap.TryGetValue(nameOrAlias, out var resolved) && !string.IsNullOrEmpty(resolved))
            {
                fileBase = resolved;
                return true;
            }

            // As a fallback, if the alias appears to already be an embedded file base, accept it
            if (_embeddedResourceIndex.ContainsKey(nameOrAlias))
            {
                fileBase = nameOrAlias;
                return true;
            }

            fileBase = string.Empty;
            return false;
        }

        private static Image? TryLoadIcon(string fileBase)
        {
            // Try embedded resource first (preferred for single-file publish)
            var embedded = TryLoadEmbeddedIcon(fileBase);
            if (embedded != null)
            {
                return embedded;
            }

            // Filesystem fallback removed; rely solely on embedded resources
            return null;
        }

        private static Image? TryLoadEmbeddedIcon(string fileBase)
        {
            try
            {
                if (_embeddedResourceIndex.TryGetValue(fileBase, out var resName)
                    || _embeddedResourceIndex.TryGetValue(Path.GetFileNameWithoutExtension(fileBase), out resName))
                {
                    var asm = Assembly.GetExecutingAssembly();
                    using var stream = asm.GetManifestResourceStream(resName);
                    if (stream != null)
                    {
                        using var temp = Image.FromStream(stream);
                        var bmp = new Bitmap(temp);
                        Trace.WriteLine($"[ShipIconService] Loaded embedded icon '{fileBase}' from '{resName}'.");
                        return bmp;
                    }
                }
                else
                {
                    var match = _embeddedResourceIndex.FirstOrDefault(kvp => kvp.Key.Equals(fileBase, StringComparison.OrdinalIgnoreCase));
                    if (!string.IsNullOrEmpty(match.Key))
                    {
                        var asm = Assembly.GetExecutingAssembly();
                        using var stream = asm.GetManifestResourceStream(match.Value);
                        if (stream != null)
                        {
                            using var temp = Image.FromStream(stream);
                            var bmp = new Bitmap(temp);
                            Trace.WriteLine($"[ShipIconService] Loaded embedded icon '{fileBase}' via loose match '{match.Value}'.");
                            return bmp;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[ShipIconService] Failed to load embedded icon for '{fileBase}': {ex.Message}");
            }
            return null;
        }

        private static Image CreateTextPlaceholder(string text, int width = 160, int height = 160)
        {
            var bmp = new Bitmap(width, height);
            try
            {
                using (var g = Graphics.FromImage(bmp))
                using (var bg = new SolidBrush(Color.FromArgb(40, 40, 40)))
                using (var border = new Pen(Color.DimGray))
                using (var brush = new SolidBrush(Color.Gainsboro))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                    g.Clear(Color.Transparent);
                    g.FillRectangle(bg, 0, 0, width, height);
                    g.DrawRectangle(border, 0, 0, width - 1, height - 1);

                    var rect = new RectangleF(8, 8, width - 16, height - 16);
                    using (var font = new Font("Segoe UI", 12f, FontStyle.Bold, GraphicsUnit.Point))
                    {
                        var sf = new StringFormat
                        {
                            Alignment = StringAlignment.Center,
                            LineAlignment = StringAlignment.Center,
                            Trimming = StringTrimming.EllipsisWord
                        };
                        g.DrawString(text, font, brush, rect, sf);
                    }
                }
                return bmp;
            }
            catch
            {
                // As a last resort, return a 1x1 transparent pixel
                bmp.Dispose();
                var tiny = new Bitmap(1, 1);
                tiny.SetPixel(0, 0, Color.Transparent);
                return tiny;
            }
        }
    }
}
