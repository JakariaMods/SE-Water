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
        public Vector3D position;
        public Vector3 velocity;
        public Vector3 leftVector;
        public Vector3 upVector;

        public int life = 1;
        public int maxLife;

        public MyEntity3DSoundEmitter cawSound;
        MySoundPair sound;

        public Seagull(Vector3D Position, Vector3D Velocity, Vector3 GravityDirection, int MaxLife = 0)
        {
            position = Position;
            velocity = Velocity;
            upVector = Vector3.Normalize(Velocity);
            leftVector = Vector3.Normalize(Vector3.Cross(GravityDirection, velocity));

            if (MaxLife == 0)
                maxLife = MyUtils.GetRandomInt(1000, 2000);
            else
                maxLife = MaxLife;

            cawSound = new MyEntity3DSoundEmitter(null);
        }

        public void Caw()
        {
            if (cawSound.IsPlaying)
                return;

            cawSound.SetPosition(position);
            cawSound.PlaySound(WaterData.SeagullSound);
            cawSound.VolumeMultiplier = WaterMod.volumeMultiplier;
        }
    }
}
