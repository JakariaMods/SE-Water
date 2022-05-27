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
using Jakaria.Utils;
using Jakaria.SessionComponents;

namespace Jakaria
{
    public class SimulatedSplash : AnimatedPointBillboard
    {
        public SimulatedSplash() { }

        private Vector3D _gravity;

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

            _initialColor = Billboard.Color;

            InScene = true;
            MyTransparentGeometry.AddBillboard(Billboard, true);

            float grav;
            _gravity = MyAPIGateway.Physics.CalculateNaturalGravityAt(position, out grav) * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS;
        }

        public override void Simulate()
        {
            Position += Velocity;
            Velocity += _gravity;

            if (WaterRenderComponent.Static.ClosestWater != null)
            {
                //Optimized method for determining if the particle is underwater
                if ((WaterRenderComponent.Static.ClosestWater.Position - Position).LengthSquared() - (WaterRenderComponent.Static.ClosestWater.Position - WaterRenderComponent.Static.CameraClosestWaterPosition).LengthSquared() + (Radius * Radius) < 0)
                {
                    MarkedForClose = true;

                    WaterEffectsComponent.Static.CreateSplash(Position, Radius * 2, true);
                }
            }
            else
            {
                MarkedForClose = true;
            }

            if (InScene) //Only update if the billboard is being drawn
            {
                MyQuadD quad;

                MyUtils.GetBillboardQuadAdvancedRotated(out quad, Position, Radius, Angle, WaterRenderComponent.Static.CameraPosition);
                Billboard.Position0 = quad.Point0;
                Billboard.Position1 = quad.Point1;
                Billboard.Position2 = quad.Point2;
                Billboard.Position3 = quad.Point3;

                Billboard.Color = Vector4.Lerp(_initialColor, Vector4.Zero, (float)Life / (float)MaxLife);
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
