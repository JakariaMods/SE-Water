using Jakaria.Components;
using Jakaria.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.ModAPI;
using VRageMath;

namespace Jakaria.Volumetrics
{
    public class VolumetricSimulation : IVolumetricSimulation
    {
        private WaterComponent _water;

        private List<IMyEntity> _entities = new List<IMyEntity>();

        private WaterTree[] _trees = new WaterTree[6]
        {
            new WaterTree(Base6Directions.Direction.Forward),
            new WaterTree(Base6Directions.Direction.Backward),
            new WaterTree(Base6Directions.Direction.Left),
            new WaterTree(Base6Directions.Direction.Right),
            new WaterTree(Base6Directions.Direction.Up),
            new WaterTree(Base6Directions.Direction.Down),
        };

        public VolumetricSimulation(WaterComponent water)
        {
            _water = water;
        }

        public float AdjustFluid(Vector3 normal, float targetAmount)
        {
            return targetAmount;
        }

        public void Dispose()
        {
            
        }

        public void GetFluid(Vector3D localNormal, out double depth, out double surface)
        {
            depth = 0;
            surface = 0;
        }

        public void Simulate()
        {
            
        }

        public class WaterTree
        {
            private Base6Directions.Direction _direction;

            public WaterTree(Base6Directions.Direction direction)
            {
                _direction = direction;
            }
        }

        public class WaterNode
        {
            public WaterNode[] Children;

            public WaterCell[] Cells;

            public const int CELLS_PER_NODE = 8;

            public void Split()
            {
                Assert.Null(Children);

                Children = new WaterNode[4]
                {
                    new WaterNode(),
                    new WaterNode(),
                    new WaterNode(),
                    new WaterNode()
                };

            }

            public void Reduce()
            {

            }
        }

        public struct WaterCell
        {
            /// <summary>
            /// The distance in meters from the center of the water source
            /// </summary>
            public double SurfaceDistanceFromCenter;

            /// <summary>
            /// The height of the fluid from the surface
            /// </summary>
            public float FluidHeight;

            /// <summary>
            /// The vertical velocity of the fluid
            /// </summary>
            public float Velocity;

            public override string ToString()
            {
                return $"<FluidHeight: {FluidHeight} SurfaceDistanceFromCenter: {SurfaceDistanceFromCenter}>";
            }
        }
    }
}
