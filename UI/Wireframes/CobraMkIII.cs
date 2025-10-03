﻿using System.Collections.Generic;
using System.Drawing;

namespace EliteDataRelay.UI
{
    public static partial class ShipWireframeData
    {
        private static WireframeGeometry CreateCobraMkIII()
        {
            return new WireframeGeometry
            {
                Polygons =
                {
                    new PointF[] { new PointF(0, -85), new PointF(25, -75), new PointF(25, -60), new PointF(-25, -60), new PointF(-25, -75) }, // Cockpit
                    new PointF[] { new PointF(-35, -60), new PointF(35, -60), new PointF(35, 70), new PointF(20, 85), new PointF(-20, 85), new PointF(-35, 70) }, // Hull
                    new PointF[] { new PointF(-35, -40), new PointF(-90, -30), new PointF(-90, 40), new PointF(-35, 50) }, // Left Wing
                    new PointF[] { new PointF(35, -40), new PointF(90, -30), new PointF(90, 40), new PointF(35, 50) }  // Right Wing
                },
                Lines = { (new PointF(0, -85), new PointF(0, 85)), (new PointF(-35, 0), new PointF(35, 0)) }
            };
        }
    }
}