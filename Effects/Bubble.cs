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
    public class Bubble
    {
        public Vector3D position;
        public float radius;
        public int life = 1;
        public int maxLife;
        public float angle;

        MyEntity3DSoundEmitter sizzleSound;

        public Bubble(Vector3D Position, float Radius = 1f, bool Audible = false, float Volume = 1f)
        {
            position = Position;
            radius = Radius * MyUtils.GetRandomFloat(0.75f, 1.25f);
            maxLife = (int)(MyUtils.GetRandomFloat(250, 500) * radius);
            angle = MyUtils.GetRandomFloat(0, 360);

            if (Audible)
            {
                sizzleSound = new MyEntity3DSoundEmitter(null);
                sizzleSound.SetPosition(position);
                sizzleSound.PlaySound(WaterData.SizzleSound);
                sizzleSound.CustomVolume = Volume;
            }
        }

        public Bubble(MyThrust thruster)
        {
            position = thruster.PositionComp.GetPosition();
            radius = thruster.CubeGrid.GridSize * MyUtils.GetRandomFloat(0.75f, 1.25f);
            maxLife = (int)(MyUtils.GetRandomFloat(500, 1000) * radius);
            angle = MyUtils.GetRandomFloat(0, 360);
        }
    }
}
