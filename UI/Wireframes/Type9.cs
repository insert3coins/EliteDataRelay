using System.Collections.Generic;
using System.Drawing;

namespace EliteDataRelay.UI
{
    public static partial class ShipWireframeData
    {
        private static WireframeGeometry CreateType9()
        {
            return new WireframeGeometry
            {
                Polygons = { new PointF[] { new PointF(-80, -90), new PointF(80, -90), new PointF(80, 90), new PointF(-80, 90) } },
                Lines = { (new PointF(0, -90), new PointF(0, 90)), (new PointF(-80, 0), new PointF(80, 0)) }
            };
        }
    }
}