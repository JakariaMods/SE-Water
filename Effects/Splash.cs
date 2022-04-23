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
    public class Splash
    {
        public Vector3D position;
        public float radius;

        public int life = 1;
        public int maxLife;

        MyEntity3DSoundEmitter splashSound;

        public Splash(Vector3D Position, float Radius = 1f, float Volume = 1f)
        {
            position = Position;
            radius = Radius;
            maxLife = (int)MyUtils.GetRandomFloat(60, 80);
            
            if (Volume != 0)
            {
                splashSound = new MyEntity3DSoundEmitter(null);
                splashSound.SetPosition(position);

                if(WaterMod.Session.CameraUnderwater)
                    splashSound.PlaySound(WaterData.UnderwaterSplashSound);
                else
                    splashSound.PlaySound(WaterData.SplashSound);

                splashSound.CustomVolume = Volume * WaterMod.Settings.Volume * ((25f - Math.Max(WaterMod.Session.InsideGrid - 10, 0)) / 25f);
            }
        }
    }
}
