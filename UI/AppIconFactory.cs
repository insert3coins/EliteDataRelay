using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace EliteDataRelay.UI
{
    public static class AppIconFactory
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        public static Icon CreateAppIcon(int size = 32)
        {
            var bmp = CreateBitmap(size);

            // Convert to icon and clone so we can free HICON
            var hIcon = bmp.GetHicon();
            var icon = Icon.FromHandle(hIcon);
            var clone = (Icon)icon.Clone();
            DestroyIcon(hIcon);
            icon.Dispose();
            bmp.Dispose();
            return clone;
        }

        public static Bitmap CreateBitmap(int size)
        {
            var bmp = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bmp))
            {
                GameColors.ConfigureHighQuality(g);
                g.Clear(Color.Transparent);

                // Background rounded rect
                var rect = new Rectangle(0, 0, size - 1, size - 1);
                using (var path = DrawingUtils.CreateRoundedRectPath(rect, Math.Max(3, size / 6)))
                using (var bg = new SolidBrush(GameColors.BackgroundDark))
                using (var border = new Pen(GameColors.BorderColor, Math.Max(1f, size / 24f)))
                {
                    g.FillPath(bg, path);
                    g.DrawPath(border, path);
                }

                // Elite-styled orange arc (top-left to bottom-right)
                float arcInset = size * 0.18f;
                var arcRect = new RectangleF(arcInset, arcInset, size - arcInset * 2, size - arcInset * 2);
                using (var orange = new Pen(GameColors.Orange, Math.Max(2f, size / 10f)))
                {
                    orange.StartCap = LineCap.Round;
                    orange.EndCap = LineCap.Round;
                    g.DrawArc(orange, arcRect, 210, 120);
                }

                // Cyan chevron pointer (navigation vibe)
                using (var cyan = new Pen(GameColors.Cyan, Math.Max(2f, size / 12f)))
                {
                    cyan.StartCap = LineCap.Round;
                    cyan.EndCap = LineCap.Round;
                    var cx = size * 0.62f;
                    var cy = size * 0.58f;
                    g.DrawLine(cyan, cx - size * 0.18f, cy, cx, cy - size * 0.18f);
                    g.DrawLine(cyan, cx, cy - size * 0.18f, cx + size * 0.18f, cy + size * 0.02f);
                }

                // Subtle inner glow
                using (var glow = new Pen(Color.FromArgb(40, 255, 255, 255), Math.Max(1f, size / 24f)))
                {
                    g.DrawEllipse(glow, size * 0.1f, size * 0.1f, size * 0.8f, size * 0.8f);
                }

                if (size >= 48)
                {
                    using (var f = new Font("Consolas", size / 5f, FontStyle.Bold, GraphicsUnit.Pixel))
                    using (var br = new SolidBrush(GameColors.White))
                    {
                        var s = "EDR";
                        var ts = g.MeasureString(s, f);
                        g.DrawString(s, f, br, (size - ts.Width) / 2f, size * 0.62f);
                    }
                }
            }
            return bmp;
        }

        public static void ExportIco(string outputPath)
        {
            // Generate multiple sizes and write a PNG-framed .ico
            int[] sizes = new[] { 16, 24, 32, 48, 64, 128, 256 };
            using (var fs = System.IO.File.Create(outputPath))
            using (var bw = new System.IO.BinaryWriter(fs))
            {
                // ICONDIR
                bw.Write((ushort)0); // reserved
                bw.Write((ushort)1); // type: icon
                bw.Write((ushort)sizes.Length); // count

                long dirEntryPos = bw.BaseStream.Position;
                // Placeholder for directory entries
                for (int i = 0; i < sizes.Length; i++)
                {
                    bw.Write((byte)0); // width
                    bw.Write((byte)0); // height
                    bw.Write((byte)0); // color count
                    bw.Write((byte)0); // reserved
                    bw.Write((ushort)0); // planes
                    bw.Write((ushort)32); // bitcount
                    bw.Write(0); // bytes in res
                    bw.Write(0); // offset
                }

                // Write images and record their offsets/sizes
                long[] offsets = new long[sizes.Length];
                int[] lengths = new int[sizes.Length];
                for (int i = 0; i < sizes.Length; i++)
                {
                    offsets[i] = bw.BaseStream.Position;
                    using (var bmp = CreateBitmap(sizes[i]))
                    using (var ms = new System.IO.MemoryStream())
                    {
                        // Store as PNG data inside ICO (supported by Windows)
                        bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        var bytes = ms.ToArray();
                        bw.Write(bytes);
                        lengths[i] = bytes.Length;
                    }
                }

                // Seek back to fill in directory
                bw.BaseStream.Seek(dirEntryPos, System.IO.SeekOrigin.Begin);
                for (int i = 0; i < sizes.Length; i++)
                {
                    byte w = (byte)(sizes[i] == 256 ? 0 : sizes[i]);
                    byte h = w; // square
                    bw.Write(w);
                    bw.Write(h);
                    bw.Write((byte)0); // color count
                    bw.Write((byte)0); // reserved
                    bw.Write((ushort)1); // planes (set to 1 for compatibility)
                    bw.Write((ushort)32); // bitcount
                    bw.Write(lengths[i]); // bytes in res
                    bw.Write((int)offsets[i]); // offset
                }
            }
        }
    }
}
