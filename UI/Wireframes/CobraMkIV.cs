using System.Collections.Generic;
using System.Drawing;

namespace EliteDataRelay.UI
{
    public static partial class ShipWireframeData
    {
        private static WireframeGeometry CreateCobraMkIV()
        {
            return new WireframeGeometry
            {
                Polygons =
                {
                    new PointF[] { new PointF(0, -80), new PointF(30, -70), new PointF(30, -55), new PointF(-30, -55), new PointF(-30, -70) },
                    new PointF[] { new PointF(-40, -55), new PointF(40, -55), new PointF(40, 60), new PointF(25, 80), new PointF(-25, 80), new PointF(-40, 60) },
                    new PointF[] { new PointF(-40, -30), new PointF(-95, -20), new PointF(-95, 30), new PointF(-40, 40) },
                    new PointF[] { new PointF(40, -30), new PointF(95, -20), new PointF(95, 30), new PointF(40, 40) }
                }
            };
        }
    }
}