using System.Collections.Generic;
using System.Drawing;

namespace EliteDataRelay.UI
{
    public static partial class ShipWireframeData
    {
        private static WireframeGeometry CreateAdder()
        {
            return new WireframeGeometry
            {
                Polygons = { new PointF[] { new PointF(-50, -70), new PointF(50, -70), new PointF(50, 70), new PointF(-50, 70) } },
                Lines = { (new PointF(-30, -70), new PointF(-30, 70)), (new PointF(30, -70), new PointF(30, 70)), (new PointF(-50, 0), new PointF(50, 0)) }
            };
        }
    }
}