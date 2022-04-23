using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Utils;
using VRageMath;

namespace Jakaria.Utils
{
    public struct QuadBillboard
    {
        public MyStringId Material;
        public MyQuadD Quad;
        public Vector4 Color;

        public QuadBillboard(MyStringId Material, ref MyQuadD Quad, ref Vector4 Color)
        {
            this.Material = Material;
            this.Quad = Quad;
            this.Color = Color;
        }

        public QuadBillboard(ref MyStringId Material, ref MyQuadD Quad, Vector4 Color)
        {
            this.Material = Material;
            this.Quad = Quad;
            this.Color = Color;
        }

        public QuadBillboard(ref MyStringId Material, ref MyQuadD Quad, ref Vector4 Color)
        {
            this.Material = Material;
            this.Quad = Quad;
            this.Color = Color;
        }

        public QuadBillboard(MyStringId Material, ref MyQuadD Quad, Vector4 Color)
        {
            this.Material = Material;
            this.Quad = Quad;
            this.Color = Color;
        }
    }
}
