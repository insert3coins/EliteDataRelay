using System.Collections.Generic;
using System.Drawing;

namespace EliteDataRelay.UI
{
    public static partial class ShipWireframeData
    {
        private static WireframeGeometry CreateImperialCourier()
        {
            return new WireframeGeometry
            {
                Polygons =
                {
                    new PointF[] { new PointF(0, -80), new PointF(25, 0), new PointF(0, 20), new PointF(-25, 0) },
                    new PointF[] { new PointF(-25, -10), new PointF(-90, -10), new PointF(-25, 50) },
                    new PointF[] { new PointF(25, -10), new PointF(90, -10), new PointF(25, 50) }
                },
                Lines = { (new PointF(0, 20), new PointF(0, 80)) }
            };
        }
    }
}