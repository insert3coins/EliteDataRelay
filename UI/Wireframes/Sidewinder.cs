using System.Collections.Generic;
using System.Drawing;

namespace EliteDataRelay.UI
{
    public static partial class ShipWireframeData
    {
        private static WireframeGeometry CreateSidewinder()
        {
            return new WireframeGeometry
            {
                Polygons =
                {
                    new PointF[] { new PointF(0, -80), new PointF(30, -60), new PointF(30, 70), new PointF(-40, 70), new PointF(-40, -60) }, // Hull
                    new PointF[] { new PointF(-40, -50), new PointF(-70, -40), new PointF(-70, 40), new PointF(-40, 50) }, // Left Pod
                    new PointF[] { new PointF(30, -50), new PointF(60, -40), new PointF(60, 40), new PointF(30, 50) }  // Right Pod
                },
                Lines = { (new PointF(0, -80), new PointF(0, -95)), (new PointF(0, -95), new PointF(-15, -85)), (new PointF(0, -80), new PointF(0, 70)) }
            };
        }
    }
}