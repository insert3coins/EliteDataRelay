using System.Collections.Generic;
using System.Drawing;

namespace EliteDataRelay.UI
{
    public static partial class ShipWireframeData
    {
        private static WireframeGeometry CreateFederalDropship()
        {
            return new WireframeGeometry
            {
                Polygons =
                {
                    new PointF[] { new PointF(0, -90), new PointF(40, -70), new PointF(40, 80), new PointF(-40, 80), new PointF(-40, -70) },
                    new PointF[] { new PointF(-40, 0), new PointF(-60, 20), new PointF(-60, 60), new PointF(-40, 70) },
                    new PointF[] { new PointF(40, 0), new PointF(60, 20), new PointF(60, 60), new PointF(40, 70) }
                }
            };
        }
    }
}