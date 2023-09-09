using Jakaria.Components;
using Jakaria.SessionComponents;
using Jakaria.Utils;
using Sandbox.Game.Components;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace Jakaria.Effects
{
    public class Wake : IDisposable
    {
        private WaterRenderSessionComponent _renderComponent;

        private const int MAX_LIFE = 60;

        private Vector3D _velocity;
        private Vector3 _normal;

        private MyBillboard _billboard;

        private int _life;

        private Vector4 _initialColor;

        public bool InScene;
        public bool MarkedForClose;

        public Wake(WaterRenderSessionComponent renderComponent, WaterComponent water, Vector3D worldPosition, Vector3D horizontalVelocity, float radius)
        {
            _renderComponent = renderComponent;
            _initialColor = WaterData.WakeColor;
            _velocity = horizontalVelocity * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS;

            Vector3 axisA = WaterUtils.GetPerpendicularVector(renderComponent.CameraGravityDirection, MyUtils.GetRandomDouble(-Math.PI, Math.PI));
            Vector3 axisB = axisA.Cross(renderComponent.CameraGravityDirection);

            _billboard = new MyBillboard()
            {
                Color = _initialColor,
                CustomViewProjection = -1,
                Material = WaterData.FoamMaterial,
                UVSize = WaterData.FoamUVSize,
                UVOffset = new Vector2(MyUtils.GetRandomInt(0, 4) / 4f, 0f),
                Position0 = water.GetClosestSurfacePointGlobal(worldPosition + ((axisA - axisB) * radius)),
                Position1 = water.GetClosestSurfacePointGlobal(worldPosition + ((axisA + axisB) * radius)),
                Position2 = water.GetClosestSurfacePointGlobal(worldPosition + ((-axisA + axisB) * radius)),
                Position3 = water.GetClosestSurfacePointGlobal(worldPosition + ((-axisA - axisB) * radius)),
                Reflectivity = 0,
                ColorIntensity = _renderComponent.AmbientColorIntensity
            };
            _normal = Vector3.Normalize((Vector3)Vector3D.Cross(_billboard.Position2 - _billboard.Position0, _billboard.Position1 - _billboard.Position0));

            MyTransparentGeometry.AddBillboard(_billboard, true);
            InScene = true;
        }

        public virtual void Simulate()
        {
            _life++;

            if (_life > MAX_LIFE)
            {
                MarkedForClose = true;
            }

            if (InScene)
            {
                if (MarkedForClose)
                {
                    MyTransparentGeometry.RemovePersistentBillboard(_billboard);
                    InScene = false;
                    return;
                }

                if (!Vector3.IsZero(_velocity))
                {
                    _billboard.Position0 += _velocity;
                    _billboard.Position1 += _velocity;
                    _billboard.Position2 += _velocity;
                    _billboard.Position3 += _velocity;

                    _velocity *= 0.97f;
                }

                float dot = Math.Min(Math.Max(Vector3.Dot(_normal, Vector3.Normalize(_billboard.Position0 - _renderComponent.CameraPosition)) + 0.1f, 0), 1);
                //MyAPIGateway.Utilities.ShowNotification($"{dot}", 30);
                _billboard.Color = Vector4.Lerp(_initialColor, Vector4.Zero, ((float)_life / (float)MAX_LIFE));
                _billboard.Color = Vector4.Lerp(Vector4.Zero, _billboard.Color, dot);
            }
        }

        public void Dispose()
        {
            if (InScene)
            {
                MyTransparentGeometry.RemovePersistentBillboard(_billboard);
                InScene = false;
                MarkedForClose = true;
            }
        }
    }
}
