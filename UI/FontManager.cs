using System;
using System.Drawing;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using EliteDataRelay.Configuration;

namespace EliteDataRelay.UI
{
    /// <summary>
    /// Manages loading and providing application-specific fonts.
    /// </summary>
    public class FontManager : IDisposable
    {
        private PrivateFontCollection? _privateFonts;
        private IntPtr _fontMemoryPtr = IntPtr.Zero;

        public Font VerdanaFont { get; private set; } = null!;
        public Font ConsolasFont { get; private set; } = null!;
        public Font AnimationFont { get; private set; } = null!;

        public FontManager()
        {
            LoadCustomFonts();
            LoadSystemFonts();
        }

        private void LoadCustomFonts()
        {
            try
            {
                // Initialize Verdana font from embedded resources.
                byte[] fontData = Properties.Resources.VerdanaFont;

                // Allocate unmanaged memory and copy font data. This memory must not be freed until the PrivateFontCollection is disposed.
                _fontMemoryPtr = Marshal.AllocCoTaskMem(fontData.Length);
                Marshal.Copy(fontData, 0, _fontMemoryPtr, fontData.Length);

                _privateFonts = new PrivateFontCollection();
                _privateFonts.AddMemoryFont(_fontMemoryPtr, fontData.Length);

                VerdanaFont = new Font(_privateFonts.Families[0], AppConfiguration.DefaultFontSize);
            }
            catch
            {
                // Fallback to default font if custom font fails
                VerdanaFont = new Font(FontFamily.GenericSansSerif, AppConfiguration.DefaultFontSize);
                _privateFonts?.Dispose();
                if (_fontMemoryPtr != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(_fontMemoryPtr);
                    _fontMemoryPtr = IntPtr.Zero;
                }
            }
        }

        private void LoadSystemFonts()
        {
            // Initialize Consolas font from system
            try
            {
                ConsolasFont = new Font(AppConfiguration.ConsolasFontName, AppConfiguration.DefaultFontSize);
                AnimationFont = new Font(AppConfiguration.ConsolasFontName, 12f); // Larger font for animation
            }
            catch
            {
                ConsolasFont = new Font(FontFamily.GenericMonospace, AppConfiguration.DefaultFontSize);
                AnimationFont = new Font(FontFamily.GenericMonospace, 12f); // Fallback for animation font
            }
        }

        public void Dispose()
        {
            VerdanaFont?.Dispose();
            ConsolasFont?.Dispose();
            AnimationFont?.Dispose();
            _privateFonts?.Dispose();
            if (_fontMemoryPtr != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(_fontMemoryPtr);
                _fontMemoryPtr = IntPtr.Zero;
            }
        }
    }
}