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
using Jakaria.Components;
namespace Jakaria
{
    public class SimulatedSplash : AnimatedPointBillboard
    {
        public SimulatedSplash() { }

        public SimulatedSplash(Vector3D position, Vector3D velocity, float radius, Water water)
        {
            Position = position;
            Velocity = velocity * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS;
            MaxLife = 200;

            Radius = radius;
            Angle = MyUtils.GetRandomInt(0,360);

            Billboard = new MyBillboard()
            {
                Color = WaterData.WhiteColor,
                CustomViewProjection = -1,
                ColorIntensity = water.PlanetConfig.ColorIntensity,
                Material = WaterData.PhysicalSplashMaterial,
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
            Velocity += WaterModComponent.Session.Gravity * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS;

            if (WaterModComponent.Session.ClosestWater != null)
            {
                //Optimized method for determining if the particle is underwater
                if ((WaterModComponent.Session.ClosestWater.Position - Position).LengthSquared() - (WaterModComponent.Session.ClosestWater.Position - WaterModComponent.Session.CameraClosestWaterPosition).LengthSquared() + (Radius * Radius) < 0)
                {
                    MarkedForClose = true;

                    WaterModComponent.Static.CreateSplash(Position, Radius * 2, true);
                }
            }
            else
            {
                MarkedForClose = true;
            }

            if (InScene) //Only update if the billboard is being drawn
            {
                MyQuadD quad;

                MyUtils.GetBillboardQuadAdvancedRotated(out quad, Position, Radius, Angle, WaterModComponent.Session.CameraPosition);
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
