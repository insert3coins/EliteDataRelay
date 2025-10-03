using System.Collections.Generic;
using System.Drawing;

namespace EliteDataRelay.UI
{
    public static partial class ShipWireframeData
    {
        private static WireframeGeometry CreateEagle()
        {
            return new WireframeGeometry
            {
                Polygons = { new PointF[] { new PointF(0, -90), new PointF(15, 0), new PointF(0, 20), new PointF(-15, 0) } },
                Lines =
                {
                    (new PointF(0, 20), new PointF(0, 80)), (new PointF(-15, 0), new PointF(-60, 30)),
                    (new PointF(15, 0), new PointF(60, 30)), (new PointF(-10, 80), new PointF(10, 80))
                }
            };
        }
    }
}