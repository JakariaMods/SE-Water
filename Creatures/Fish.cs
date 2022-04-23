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
    public class Fish : AnimatedBillboard
    {
        Vector3D AnimationNormal;

        public Fish() { }

        public Fish(Vector3D position, Vector3D velocity, Vector3D gravityDirection, int maxLife, float size)
        {
            Vector3 leftVector = Vector3.Normalize(velocity);
            Velocity = velocity * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS;
            MaxLife = maxLife;

            Billboard = new MyBillboard()
            {
                Color = Vector4.One,
                CustomViewProjection = -1,
                ColorIntensity = 1f,
                Material = WaterData.FishMaterial,
                UVSize = WaterData.FishUVSize,
                UVOffset = new Vector2(WaterData.FishUVSize.X * MyUtils.GetRandomInt(0, 4), WaterData.FishUVSize.Y * MyUtils.GetRandomInt(0, 2)),
                Position0 = position + leftVector * size,
                Position1 = position,
                Position2 = position + gravityDirection * size,
                Position3 = position + ((leftVector + gravityDirection) * size),
            };

            MyTransparentGeometry.AddBillboard(Billboard, true);
            InScene = true;

            AnimationNormal = Vector3D.Cross(leftVector, gravityDirection);
        }

        public override void Simulate()
        {
            Vector3D Animation = AnimationNormal * Math.Sin(MyAPIGateway.Session.ElapsedPlayTime.TotalSeconds * 5) * 0.003;
            
            Billboard.Position0 -= Animation;
            Billboard.Position1 += Animation;
            Billboard.Position2 += Animation;
            Billboard.Position3 -= Animation;

            base.Simulate();
        }
    }
}
