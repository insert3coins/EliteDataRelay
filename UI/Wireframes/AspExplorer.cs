using System.Collections.Generic;
using System.Drawing;

namespace EliteDataRelay.UI
{
    public static partial class ShipWireframeData
    {
        private static WireframeGeometry CreateAspExplorer()
        {
            return new WireframeGeometry
            {
                Polygons =
                {
                    new PointF[] { new PointF(0, -50), new PointF(40, -40), new PointF(40, 40), new PointF(-40, 40), new PointF(-40, -40) },
                    new PointF[] { new PointF(-40, -30), new PointF(-90, -30), new PointF(-90, 30), new PointF(-40, 30) },
                    new PointF[] { new PointF(40, -30), new PointF(90, -30), new PointF(90, 30), new PointF(40, 30) }
                }
            };
        }
    }
}