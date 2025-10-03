using System.Collections.Generic;
using System.Drawing;

namespace EliteDataRelay.UI
{
    public static partial class ShipWireframeData
    {
        private static WireframeGeometry CreateDolphin()
        {
            return new WireframeGeometry
            {
                Polygons = { new PointF[] { new PointF(0, -90), new PointF(40, 0), new PointF(0, 90), new PointF(-40, 0) } },
                Lines = { (new PointF(-20, -70), new PointF(20, -70)), (new PointF(0, 90), new PointF(0, 100)) }
            };
        }
    }
}