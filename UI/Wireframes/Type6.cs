using System.Collections.Generic;
using System.Drawing;

namespace EliteDataRelay.UI
{
    public static partial class ShipWireframeData
    {
        private static WireframeGeometry CreateType6()
        {
            return new WireframeGeometry
            {
                Polygons = { new PointF[] { new PointF(-60, -80), new PointF(60, -80), new PointF(60, 80), new PointF(-60, 80) } },
                Lines = { (new PointF(-40, -80), new PointF(-40, 80)), (new PointF(40, -80), new PointF(40, 80)) }
            };
        }
    }
}