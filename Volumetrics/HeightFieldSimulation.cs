using Jakaria.Components;
using Jakaria.SessionComponents;
using Jakaria.Utils;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace Jakaria.Volumetrics
{
    public class HeightFieldSimulation : IVolumetricSimulation
    {
        private int _cellPrecision = 64;

        private WaterComponent _water;
        private WaterGrid _waterGrid;

        public HeightFieldSimulation(WaterComponent water)
        {
            _water = water;

            /*float diameter = ((MyObjectBuilder_Planet)_water.Planet.GetObjectBuilder()).Radius * 2;
            int size = Math.Max((int)Math.Ceiling(diameter / _cellPrecision), 1);

            WaterUtils.ShowMessage($"Dia: {diameter} Size: {size}");*/
            _waterGrid = new WaterGrid(128);

            for (int x = 0; x < _waterGrid.Width; x++)
            {
                for (int y = 0; y < _waterGrid.Height; y++)
                {
                    int index = _waterGrid.GetIndex(x, y);

                    Vector3 localPos = -ToNormal(new Vector2I(x, y)) * _water.Planet.AverageRadius;
                    double radius = _water.Planet.GetClosestSurfacePointLocal(ref localPos).Length();

                    _waterGrid.Nodes[index] = new WaterNode
                    {
                        SurfaceDistanceFromCenter = radius,
                        FluidHeight = (float)Math.Max(_water.Radius - radius, 0)
                    };
                }
            }
        }

        public void Simulate()
        {
            MyAPIGateway.Utilities.ShowNotification($"Size: {_waterGrid.Width} {_waterGrid.Height}", 16);
            MyAPIGateway.Utilities.ShowNotification($"Surf: {ToSurface(Session.Instance.Get<WaterRenderSessionComponent>().CameraGravityDirection)}", 16);
            MyAPIGateway.Utilities.ShowNotification($"Normal: {ToNormal(ToSurface(Session.Instance.Get<WaterRenderSessionComponent>().CameraGravityDirection))}", 16);
            MyAPIGateway.Utilities.ShowNotification($"{ _waterGrid.Nodes[_waterGrid.GetIndex(ToSurface(Session.Instance.Get<WaterRenderSessionComponent>().CameraGravityDirection))]}", 16);

            /*for (int x = 0; x < _waterGrid.Width; x++)
            {
                for (int y = 0; y < _waterGrid.Height; y++)
                {
                    int index = _waterGrid.GetIndex(x, y);

                    int top = _waterGrid.GetIndex(x, y + 1);
                    int bottom = _waterGrid.GetIndex(x, y - 1);
                    int left = _waterGrid.GetIndex(x - 1, y);
                    int right = _waterGrid.GetIndex(x + 1, y);
                }
            }*/
        }

        public float AdjustFluid(Vector3 normal, float targetAmount)
        {
            return 0;
        }

        public void GetFluid(Vector3D localNormal, out double depth, out double surface)
        {
            var node = _waterGrid.Nodes[_waterGrid.GetIndex(ToSurface(localNormal))];

            depth = node.FluidHeight;
            surface = node.SurfaceDistanceFromCenter;
        }

        public void Dispose()
        {
            
        }

        private Vector2I ToSurface(Vector3D normal)
        {
            double longitude = Math.Atan2(normal.Y, normal.X);
            double hyp = Math.Sqrt(normal.X * normal.X + normal.Y * normal.Y);
            double latitude = Math.Atan2(normal.Z, hyp);

            latitude += Math.PI;
            longitude += Math.PI;

            latitude /= (Math.PI * 2);
            longitude /= (Math.PI * 2);

            latitude *= _waterGrid.Width;
            longitude *= _waterGrid.Height;

            return new Vector2I(MathHelper.RoundToInt(latitude), MathHelper.RoundToInt(longitude));
        }

        public Vector3D ToNormal(Vector2I latLon)
        {
            double latitude = latLon.X;
            double longitude = latLon.Y;

            latitude /= _waterGrid.Width;
            longitude /= _waterGrid.Height;

            latitude *= (Math.PI * 2);
            longitude *= (Math.PI * 2);

            latitude -= Math.PI;
            longitude -= Math.PI;

            double x = Math.Cos(latitude) * Math.Cos(longitude);
            double y = Math.Cos(latitude) * Math.Sin(longitude);
            double z = Math.Sin(latitude);

            return new Vector3D(x, y, z);
        }
    }

    public struct WaterGrid
    {
        /// <summary>
        /// Grid of <see cref="WaterNode"/>s. Always square shaped.
        /// </summary>
        public WaterNode[] Nodes;
        public int Width;
        public int Height;

        public WaterGrid(int size)
        {
            Width = size << 1;
            Height = size;
            Nodes = new WaterNode[Width * Height];
        }

        public int GetIndex(Vector2I surface)
        {
            return GetIndex(surface.X, surface.Y);
        }

        public int GetIndex(int x, int y)
        {
            if (x >= Width)
                x -= Width;
            else if (x < 0)
                x += Width;

            if (y >= Height)
                y -= Height;
            else if(y < 0)
                y += Height;

            return (Height * y) + x;
        }
    }
}
