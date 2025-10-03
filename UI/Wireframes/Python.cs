using System.Collections.Generic;
using System.Drawing;

namespace EliteDataRelay.UI
{
    public static partial class ShipWireframeData
    {
        private static WireframeGeometry CreatePython()
        {
            return new WireframeGeometry
            {
                Polygons = { new PointF[] { new PointF(0, -100), new PointF(50, 0), new PointF(30, 90), new PointF(-30, 90), new PointF(-50, 0) } },
                Lines = { (new PointF(0, -100), new PointF(0, 90)), (new PointF(-50, 0), new PointF(50, 0)) }
            };
        }
    }
}