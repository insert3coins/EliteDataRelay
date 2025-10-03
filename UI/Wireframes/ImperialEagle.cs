using System.Collections.Generic;
using System.Drawing;

namespace EliteDataRelay.UI
{
    public static partial class ShipWireframeData
    {
        private static WireframeGeometry CreateImperialEagle()
        {
            return new WireframeGeometry
            {
                Polygons = { new PointF[] { new PointF(0, -90), new PointF(20, 20), new PointF(0, 50), new PointF(-20, 20) } },
                Lines =
                {
                    (new PointF(-20, 20), new PointF(-80, 40)), (new PointF(20, 20), new PointF(80, 40)),
                    (new PointF(0, 50), new PointF(0, 90)), (new PointF(-15, 90), new PointF(15, 90))
                }
            };
        }
    }
}