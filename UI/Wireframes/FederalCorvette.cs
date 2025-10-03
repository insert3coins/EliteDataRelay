using System.Collections.Generic;
using System.Drawing;

namespace EliteDataRelay.UI
{
    public static partial class ShipWireframeData
    {
        private static WireframeGeometry CreateFederalCorvette()
        {
            return new WireframeGeometry
            {
                Polygons = { new PointF[] { new PointF(0, -110), new PointF(40, 20), new PointF(20, 100), new PointF(-20, 100), new PointF(-40, 20) } },
                Lines = { (new PointF(-30, 50), new PointF(-80, 70)), (new PointF(30, 50), new PointF(80, 70)) }
            };
        }
    }
}