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
using Jakaria.SessionComponents;
using Jakaria.Components;

namespace Jakaria
{
    public class SimulatedSplash : AnimatedPointBillboard
    {

        private Vector3D _gravity;

        private readonly WaterRenderSessionComponent _renderComponent;

        public SimulatedSplash(Vector3D position, Vector3D velocity, float radius, WaterComponent water)
        {
            Position = position;
            Velocity = velocity * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS;
            MaxLife = 200;

            Radius = radius;
            Angle = MyUtils.GetRandomInt(0,360);

            _renderComponent = Session.Instance.Get<WaterRenderSessionComponent>();

            Billboard = new MyBillboard()
            {
                Color = WaterData.WhiteColor,
                CustomViewProjection = -1,
                ColorIntensity = water.PlanetConfig.ColorIntensity * _renderComponent.AmbientColorIntensity,
                Material = WaterData.PhysicalSplashMaterial,
                UVSize = Vector2.One,
                UVOffset = Vector2.Zero,
            };

            _initialColor = Billboard.Color;

            InScene = true;
            MyTransparentGeometry.AddBillboard(Billboard, true);

            float grav;
            _gravity = MyAPIGateway.Physics.CalculateNaturalGravityAt(position, out grav) * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS / 10f;
        }

        public override void Simulate()
        {
            Position += Velocity;
            Velocity += _gravity;

            if (_renderComponent.ClosestWater != null)
            {
                if (_renderComponent.ClosestWater.IsUnderwaterGlobal(ref Position))
                {
                    MarkedForClose = true;

                    Session.Instance.Get<WaterEffectsComponent>().CreateSplash(Position, Radius * 2, true);
                }
            }
            else
            {
                MarkedForClose = true;
            }

            if (InScene) //Only update if the billboard is being drawn
            {
                MyQuadD quad;

                MyUtils.GetBillboardQuadAdvancedRotated(out quad, Position, Radius, Angle, _renderComponent.CameraPosition);
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
