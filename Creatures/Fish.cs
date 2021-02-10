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
using Jakaria;

namespace Jakaria
{
    public class Fish
    {
        public Vector3D Position;
        public Vector3D Velocity;

        public Vector3 LeftVector { protected set; get; }
        public Vector3 UpVector { protected set; get; }

        public int Life { set; get; } = 0;
        public int MaxLife { protected set; get; }

        public byte textureId;

        public Fish(Vector3D Position, Vector3D Velocity, Vector3 GravityDirection, int MaxLife = 0)
        {
            this.Position = Position;
            this.Velocity = Velocity;
            textureId = (byte)MyUtils.GetRandomInt(0, WaterData.FishMaterials.Length);
            this.LeftVector = Vector3.Normalize(Velocity);
            this.UpVector = Vector3.Normalize(-GravityDirection);

            if (MaxLife == 0)
                this.MaxLife = MyUtils.GetRandomInt(1000, 2000);
            else
                this.MaxLife = MaxLife;
        }
    }
}
