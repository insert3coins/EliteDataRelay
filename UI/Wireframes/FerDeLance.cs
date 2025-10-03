using System.Collections.Generic;
using System.Drawing;

namespace EliteDataRelay.UI
{
    public static partial class ShipWireframeData
    {
        private static WireframeGeometry CreateFerDeLance()
        {
            return new WireframeGeometry
            {
                Polygons = { new PointF[] { new PointF(0, -90), new PointF(80, 20), new PointF(0, 60), new PointF(-80, 20) } },
                Lines = { (new PointF(0, -90), new PointF(0, 60)) }
            };
        }
    }
}