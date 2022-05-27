using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Utils;
using VRageMath;
using Jakaria.Components;
using VRageRender;
using Sandbox.ModAPI;
using VRage.Game;
using Jakaria.SessionComponents;

namespace Jakaria
{
    public class Splash
    {
        public Vector3D Position;
        public float Radius;

        public int Life = 1;
        public int MaxLife;

        public MyBillboard Billboard;

        private MyEntity3DSoundEmitter _splashSound;

        public Splash(Vector3D position, float radius = 1f, float volume = 1f)
        {
            this.Position = position;
            this.Radius = radius;
            MaxLife = (int)MyUtils.GetRandomFloat(60, 80);

            Billboard = new MyBillboard()
            {
                Material = WaterData.SplashMaterial,
                CustomViewProjection = -1,
                ColorIntensity = 1,
                UVSize = Vector2.One,
            };

            MyTransparentGeometry.AddBillboard(Billboard, true);

            if (volume != 0)
            {
                _splashSound = new MyEntity3DSoundEmitter(null);
                _splashSound.SetPosition(this.Position);

                MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                {
                    if (WaterRenderComponent.Static.CameraUnderwater)
                        _splashSound.PlaySound(WaterData.UnderwaterSplashSound);
                    else
                        _splashSound.PlaySound(WaterData.SplashSound);
                });

                _splashSound.CustomVolume = volume * WaterSoundComponent.Static.VolumeMultiplier;
            }
        }
    }
}
