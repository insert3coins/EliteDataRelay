using System.Collections.Generic;
using System.Drawing;

namespace EliteDataRelay.UI
{
    public static partial class ShipWireframeData
    {
        private static WireframeGeometry CreateChieftain()
        {
            return new WireframeGeometry
            {
                Polygons =
                {
                    new PointF[] { new PointF(0, -80), new PointF(20, -70), new PointF(60, 40), new PointF(40, 80), new PointF(-40, 80), new PointF(-60, 40), new PointF(-20, -70) },
                    new PointF[] { new PointF(-25, -50), new PointF(-80, -30), new PointF(-70, 0) },
                    new PointF[] { new PointF(25, -50), new PointF(80, -30), new PointF(70, 0) }
                }
            };
        }
    }
}