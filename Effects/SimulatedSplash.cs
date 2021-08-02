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
    public class SimulatedSplash
    {
        public Vector3D position;
        public float radius;
        public Vector3D velocity;
        public float angle;
        public bool under = false;
        public int life = 1;
        public int maxLife;

        public SimulatedSplash(Vector3D Position, Vector3D Velocity, float Radius = 1f)
        {
            angle = MyUtils.GetRandomFloat(0, 360);
            position = Position;
            radius = Radius;
            maxLife = 200;
            velocity = Velocity;
        }
    }
}
