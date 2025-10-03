using System.Collections.Generic;
using System.Drawing;

namespace EliteDataRelay.UI
{
    public static partial class ShipWireframeData
    {
        private static WireframeGeometry CreateViper()
        {
            return new WireframeGeometry
            {
                Polygons =
                {
                    new PointF[] { new PointF(0, -95), new PointF(20, -20), new PointF(20, 60), new PointF(0, 80), new PointF(-20, 60), new PointF(-20, -20) }, // Hull
                    new PointF[] { new PointF(-20, -50), new PointF(-70, -30), new PointF(-60, 40), new PointF(-20, 30) }, // Left Wing
                    new PointF[] { new PointF(20, -50), new PointF(70, -30), new PointF(60, 40), new PointF(20, 30) }, // Right Wing
                    new PointF[] { new PointF(-15, 80), new PointF(15, 80), new PointF(10, 90), new PointF(-10, 90) }  // Tail
                },
                Lines = { (new PointF(0, -95), new PointF(0, 80)) }
            };
        }
    }
}