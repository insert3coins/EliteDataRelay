using System.Collections.Generic;
using System.Drawing;

namespace EliteDataRelay.UI
{
    public static partial class ShipWireframeData
    {
        private static WireframeGeometry CreateDiamondbackScout()
        {
            return new WireframeGeometry
            {
                Polygons =
                {
                    new PointF[] { new PointF(0, -60), new PointF(20, -50), new PointF(20, 60), new PointF(-20, 60), new PointF(-20, -50) },
                    new PointF[] { new PointF(-20, -20), new PointF(-80, -40), new PointF(-80, 20), new PointF(-20, 0) },
                    new PointF[] { new PointF(20, -20), new PointF(80, -40), new PointF(80, 20), new PointF(20, 0) }
                },
                Lines = { (new PointF(0, 60), new PointF(0, 80)) }
            };
        }
    }
}