using System.Collections.Generic;
using System.Drawing;

namespace EliteDataRelay.UI
{
    public static partial class ShipWireframeData
    {
        private static WireframeGeometry CreateKraitMkII()
        {
            return new WireframeGeometry
            {
                Polygons =
                {
                    new PointF[] { new PointF(-30, -80), new PointF(30, -80), new PointF(50, -20), new PointF(50, 60), new PointF(-50, 60), new PointF(-50, -20) },
                    new PointF[] { new PointF(-50, -10), new PointF(-90, -10), new PointF(-90, 10), new PointF(-50, 10) },
                    new PointF[] { new PointF(50, -10), new PointF(90, -10), new PointF(90, 10), new PointF(50, 10) }
                }
            };
        }
    }
}