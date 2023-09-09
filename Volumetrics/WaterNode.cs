using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jakaria.Volumetrics
{
    public struct WaterNode
    {
        public double SurfaceDistanceFromCenter;
        public float FluidHeight;
        public float Velocity;

        public override string ToString()
        {
            return $"<FluidHeight: {FluidHeight} SurfaceDistanceFromCenter: {SurfaceDistanceFromCenter}>";
        }

        public static int GetSize()
        {
            return sizeof(double) + sizeof(float) + sizeof(float);
        }
    }
}
