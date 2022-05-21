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
using Jakaria.Utils;
using Jakaria.Components;
namespace Jakaria
{
    public class AnimatedPointBillboard : AnimatedBillboard
    {
        public Vector3D Velocity;
        public Vector3D Position;

        public float Radius;
        public float Angle;

        public AnimatedPointBillboard() { }

        public AnimatedPointBillboard(Vector3D position, Vector3D velocity, float radius, int maxLife, float angle, MyStringId material)
        {
            Position = position;
            Velocity = velocity * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS;
            MaxLife = maxLife;

            Radius = radius;
            Angle = angle;
            
            Billboard = new MyBillboard()
            {
                Color = WaterData.SmallBubbleColor,
                CustomViewProjection = -1,
                ColorIntensity = 1f,
                Material = material,
                UVSize = Vector2.One,
                UVOffset = Vector2.Zero,
            };

            initialColor = Billboard.Color;

            InScene = true;
            MyTransparentGeometry.AddBillboard(Billboard, true);
        }

        public override void Simulate()
        {
            Position += Velocity;
            
            if (InScene) //Only update if the billboard is being drawn
            {
                MyQuadD quad;
                
                MyUtils.GetBillboardQuadAdvancedRotated(out quad, Position, Radius, Angle, WaterModComponent.Static.Session.CameraPosition);
                Billboard.Position0 = quad.Point0;
                Billboard.Position1 = quad.Point1;
                Billboard.Position2 = quad.Point2;
                Billboard.Position3 = quad.Point3;

                Billboard.Color = Vector4.Lerp(initialColor, Vector4.Zero, (float)Life / (float)MaxLife);
            }

            Life++;

            if (Life > MaxLife)
            {
                MarkedForClose = true;

                if (InScene)
                {
                    InScene = false;
                    MyTransparentGeometry.RemovePersistentBillboard(Billboard);
                }
            }
        }
    }
}
