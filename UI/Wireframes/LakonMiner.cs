﻿using System.Collections.Generic;
using System.Drawing;

namespace EliteDataRelay.UI
{
    public static partial class ShipWireframeData
    {
        private static WireframeGeometry CreateLakonMiner()
        {
            // Based on the Type-9, but with more pronounced front mandibles
            return new WireframeGeometry
            {
                Polygons = { new PointF[] { new PointF(-80, -90), new PointF(80, -90), new PointF(60, 90), new PointF(-60, 90) } },
                Lines = { (new PointF(0, -90), new PointF(0, 90)), (new PointF(-80, -70), new PointF(-40, 90)), (new PointF(80, -70), new PointF(40, 90)) }
            };
        }
    }
}