using System.Drawing;

namespace EliteDataRelay.UI
{
    public partial class StarMapPanel
    {
        // Helper class to hold data for drawing a system in 3D space
        private class DrawableSystem
        {
            public Models.StarSystem System { get; set; } = null!;
            public float RotatedX { get; set; }
            public float RotatedY { get; set; }
            public float RotatedZ { get; set; }
            public float PlaneX { get; set; }
            public float PlaneY { get; set; }
        }

        private class DrawableBackgroundStar
        {
            public Brush Brush { get; set; } = null!;
            public float RotatedX { get; set; }
            public float RotatedY { get; set; }
            public float RotatedZ { get; set; }
        }
    }
}