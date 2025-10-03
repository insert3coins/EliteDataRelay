using System.Collections.Generic;
using System.Drawing;

namespace EliteDataRelay.UI
{
    public static partial class ShipWireframeData
    {
        private static WireframeGeometry CreateViperMkIV()
        {
            return new WireframeGeometry
            {
                Polygons =
                {
                    new PointF[] { new PointF(0, -90), new PointF(30, 0), new PointF(20, 70), new PointF(-20, 70), new PointF(-30, 0) },
                    new PointF[] { new PointF(-30, -30), new PointF(-80, -10), new PointF(-70, 50), new PointF(-30, 40) },
                    new PointF[] { new PointF(30, -30), new PointF(80, -10), new PointF(70, 50), new PointF(30, 40) }
                }
            };
        }
    }
}