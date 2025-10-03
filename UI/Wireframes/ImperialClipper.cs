using System.Collections.Generic;
using System.Drawing;

namespace EliteDataRelay.UI
{
    public static partial class ShipWireframeData
    {
        private static WireframeGeometry CreateImperialClipper()
        {
            return new WireframeGeometry
            {
                Polygons =
                {
                    new PointF[] { new PointF(0, -100), new PointF(20, 0), new PointF(0, 80), new PointF(-20, 0) },
                    new PointF[] { new PointF(-20, 10), new PointF(-100, 30), new PointF(-20, 50) },
                    new PointF[] { new PointF(20, 10), new PointF(100, 30), new PointF(20, 50) }
                }
            };
        }
    }
}