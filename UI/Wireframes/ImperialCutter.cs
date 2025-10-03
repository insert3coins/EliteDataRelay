using System.Collections.Generic;
using System.Drawing;

namespace EliteDataRelay.UI
{
    public static partial class ShipWireframeData
    {
        private static WireframeGeometry CreateImperialCutter()
        {
            return new WireframeGeometry
            {
                Polygons = { new PointF[] { new PointF(0, -110), new PointF(30, 50), new PointF(0, 100), new PointF(-30, 50) } },
                Lines = { (new PointF(-30, 0), new PointF(-90, 20)), (new PointF(30, 0), new PointF(90, 20)) }
            };
        }
    }
}