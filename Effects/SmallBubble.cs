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
    public class SmallBubble
    {
        public Vector3 position;
        public float radius;
        public Vector4 color;
        public int life = 1;
        public int maxLife;
        public float angle;

        public SmallBubble(Vector3D Position, float Radius = 0.1f)
        {
            position = Position;
            radius = Radius * MyUtils.GetRandomFloat(0.75f, 1.25f);
            maxLife = (int)MyUtils.GetRandomFloat(150, 250);
            angle = MyUtils.GetRandomFloat(0, 360);
            color = new Vector4(0.15f, 0.2f, 0.25f, 0.2f);
        }
    }
}
