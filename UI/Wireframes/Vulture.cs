using System.Collections.Generic;
using System.Drawing;

namespace EliteDataRelay.UI
{
    public static partial class ShipWireframeData
    {
        private static WireframeGeometry CreateVulture()
        {
            return new WireframeGeometry
            {
                Polygons =
                {
                    new PointF[] { new PointF(0, -70), new PointF(40, -50), new PointF(40, 50), new PointF(-40, 50), new PointF(-40, -50) },
                    new PointF[] { new PointF(-40, -40), new PointF(-70, -60), new PointF(-70, 60), new PointF(-40, 40) },
                    new PointF[] { new PointF(40, -40), new PointF(70, -60), new PointF(70, 60), new PointF(40, 40) }
                }
            };
        }
    }
}