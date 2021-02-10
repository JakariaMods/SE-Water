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
    public class Seagull
    {
        public Vector3D Position;
        public Vector3D Velocity;

        public Vector3 LeftVector { protected set; get; }
        public Vector3 UpVector { protected set; get; }

        public int Life { set; get; } = 0;
        public int MaxLife { protected set; get; }

        public MyEntity3DSoundEmitter cawSound;

        public Seagull(Vector3D Position, Vector3D Velocity, Vector3 GravityDirection, int MaxLife = 0)
        {
            this.Position = Position;
            this.Velocity = Velocity;
            this.UpVector = Vector3.Normalize(Velocity);
            this.LeftVector = Vector3.Normalize(Vector3.Cross(GravityDirection, Velocity));

            if (MaxLife == 0)
                this.MaxLife = MyUtils.GetRandomInt(1000, 2000);
            else
                this.MaxLife = MaxLife;

            cawSound = new MyEntity3DSoundEmitter(null);
        }

        public void Caw()
        {
            if (cawSound.IsPlaying)
                return;

            cawSound.SetPosition(this.Position);
            cawSound.PlaySound(WaterData.SeagullSound);
            cawSound.VolumeMultiplier = WaterMod.Settings.Volume * ((25f - Math.Max(WaterMod.Session.InsideGrid - 10, 0)) / 25f);
        }
    }
}
