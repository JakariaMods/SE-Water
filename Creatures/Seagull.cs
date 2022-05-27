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
using VRageRender;
using VRage.Game;
using Sandbox.ModAPI;

namespace Jakaria
{
    public class Seagull : AnimatedBillboard
    {
        private Vector3D _animationNormal;
        private MyEntity3DSoundEmitter _soundEmitter;

        public Seagull() { }

        public Seagull(Vector3D position, Vector3D velocity, Vector3D gravityDirection, int maxLife, float size)
        {
            Vector3 leftVector = Vector3.Normalize(velocity);
            Vector3 forwardVector = Vector3.Cross(leftVector, gravityDirection);

            Velocity = velocity * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS;
            MaxLife = maxLife;

            //textureId = (byte)MyUtils.GetRandomInt(0, WaterData.FishMaterials.Length);

            Billboard = new MyBillboard()
            {
                Color = Vector4.One,
                CustomViewProjection = -1,
                ColorIntensity = 1f,
                Material = WaterData.SeagullMaterial,
                UVSize = Vector2.One,
                Position0 = position + ((leftVector + forwardVector) * size),
                Position1 = position + leftVector * size,
                Position2 = position,
                Position3 = position + forwardVector * size,
            };

            MyTransparentGeometry.AddBillboard(Billboard, true);
            InScene = true;

            _animationNormal = gravityDirection;

            _soundEmitter = new MyEntity3DSoundEmitter(null);
        }

        public override void Simulate()
        {
            Vector3D Animation = _animationNormal * Math.Sin(MyAPIGateway.Session.ElapsedPlayTime.TotalSeconds * 5) * 0.003;

            Billboard.Position0 -= Animation;
            Billboard.Position1 += Animation;
            Billboard.Position2 += Animation;
            Billboard.Position3 -= Animation;

            base.Simulate();
        }

        public void Caw()
        {
            _soundEmitter.SetPosition(Billboard.Position0);
            _soundEmitter.PlaySound(WaterData.SeagullSound, true);
        }
    }
}
