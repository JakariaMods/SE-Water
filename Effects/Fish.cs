using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Utils;
using VRageMath;
using Sandbox.Game.Entities;
using Sandbox.Game;

namespace Jakaria
{
    public class Fish
    {
        public Vector3D position;
        public Vector3 velocity;
        public Vector3 leftVector;
        public Vector3 upVector;
        public byte textureId;
        public int life = 1;
        public int maxLife;

        public Fish(Vector3D Position, Vector3D Velocity, Vector3 GravityDirection, int MaxLife = 0)
        {
            position = Position;
            velocity = Velocity;
            textureId = (byte)MyUtils.GetRandomInt(0, WaterData.FishMaterials.Length);
            leftVector = Vector3.Normalize(Velocity);
            upVector = Vector3.Normalize(-GravityDirection);

            if (MaxLife == 0)
                maxLife = MyUtils.GetRandomInt(1000, 2000);
            else
                maxLife = MaxLife;
        }
    }
}
