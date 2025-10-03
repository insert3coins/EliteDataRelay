using System.Collections.Generic;
using System.Drawing;

namespace EliteDataRelay.UI
{
    public static partial class ShipWireframeData
    {
        private static WireframeGeometry CreateHauler()
        {
            return new WireframeGeometry
            {
                Polygons = { new PointF[] { new PointF(-40, -60), new PointF(40, -60), new PointF(40, 60), new PointF(-40, 60) } },
                Lines =
                {
                    (new PointF(0, -60), new PointF(0, -80)), (new PointF(-20, -80), new PointF(20, -80)),
                    (new PointF(-40, 0), new PointF(-60, 0)), (new PointF(40, 0), new PointF(60, 0))
                }
            };
        }
    }
}