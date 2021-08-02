using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Utils;
using VRageMath;

namespace Jakaria
{
    public class GodRay
    {
        public Vector3D Position;
        public float Radius;
        
        public int Life = 1;
        public int MaxLife;

        public GodRay(Vector3D Position, float Radius = 1f)
        {
            this.Position = Position;
            this.Radius = Radius;
            this.MaxLife = (int)MyUtils.GetRandomFloat(60, 120);
        }
    }
}
