﻿using System.Collections.Generic;
using System.Drawing;

namespace EliteDataRelay.UI
{
    public static partial class ShipWireframeData
    {
        private static WireframeGeometry CreateAnaconda()
        {
            return new WireframeGeometry
            {
                Polygons = { new PointF[] { new PointF(0, -110), new PointF(25, -90), new PointF(50, 20), new PointF(35, 100), new PointF(-35, 100), new PointF(-50, 20), new PointF(-25, -90) } }, // Hull
                Lines =
                {
                    (new PointF(0, -110), new PointF(0, -80)), (new PointF(-15, -80), new PointF(15, -80)),
                    (new PointF(-50, 20), new PointF(-30, 90)), (new PointF(50, 20), new PointF(30, 90)),
                    (new PointF(0, -80), new PointF(0, 100)), (new PointF(-40, 0), new PointF(40, 0))
                }
            };
        }
    }
}