using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jakaria
{
    public struct WaterNode
    {
        public float SurfaceDistanceFromCenter;
        public float FluidHeight;

        public override string ToString()
        {
            return $"<FluidHeight: {FluidHeight} SurfaceDistanceFromCenter: {SurfaceDistanceFromCenter}>";
        }
    }
}
