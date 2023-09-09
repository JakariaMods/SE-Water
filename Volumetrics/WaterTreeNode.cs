using Jakaria.Utils;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace Jakaria.Volumetrics
{
    /// <summary>
    /// A single side/face of the <see cref="WaterTree"/>
    /// </summary>
    public class WaterTreeNode
    {
        public const int TOP_LEFT = 0;
        public const int TOP_RIGHT = 1;
        public const int BOTTOM_LEFT = 2;
        public const int BOTTOM_RIGHT = 3;

        public const double SURFACE_OFFSET = 0;

        public WaterTreeNode[] Children;

        public WaterTree Sphere;

        /// <summary>
        /// The depth the node is within the tree
        /// </summary>
        public int Depth;

        /// <summary>
        /// The diameter, out of 1, of the node
        /// </summary>
        public double Diameter;

        /// <summary>
        /// The position on the unit cube's face
        /// </summary>
        public Vector3D Position;

        /// <summary>
        /// The cube-face direction
        /// </summary>
        public Base6Directions.Direction Direction;

        /// <summary>
        /// The ID of the face within the tree, denoting how it can be traversed to locate it
        /// </summary>
        public ulong Id;

        /// <summary>
        /// The maximum split depth of the <see cref="QuadTree{T}"/>
        /// </summary>
        public const int MAX_DEPTH = 12;

        /// <summary>
        /// The minimum depth the tree is allowed to be
        /// </summary>
        public const int MIN_DEPTH = 7;

        /// <summary>
        /// If true, this or a child of this node has been edited
        /// </summary>
        public bool Edited;

        /// <summary>
        /// The distance from the center of the planet to the lowest voxel
        /// </summary>
        public double SurfaceDistanceFromCenter;
        
        /// <summary>
        /// The distance from the voxel surface to the water surface
        /// </summary>
        public double FluidHeight;

        /// <summary>
        /// Buffered version of <see cref="FluidHeight"/>
        /// </summary>
        public double NewFluidHeight;

        /// <summary>
        /// If true, the node is in the SimulatedNodes collection
        /// </summary>
        public bool Simulated;

        /// <summary>
        /// The amount the height will change in the frame
        /// </summary>
        public double Velocity;

        /// <summary>
        /// Buffered version of <see cref="Velocity"/>
        /// </summary>
        public double NewVelocity;

        /// <summary>
        /// The parent of this node, null when root
        /// </summary>
        public WaterTreeNode Parent;

        /// <summary>
        /// Cached, deepest-leaf neighbors of the quadtree
        /// </summary>
        public List<WaterTreeNode> Neighbors = new List<WaterTreeNode>(4);

        /// <summary>
        /// Constructor for root node
        /// </summary>
        public WaterTreeNode(WaterTree sphere, Base6Directions.Direction direction)
        {
            Sphere = sphere;
            Position = Base6Directions.GetVector(direction);
            Direction = direction;
            Id = 0;
            Diameter = 1.0 / Math.Pow(2, Depth);

            Vector3 surface = Vector3.Normalize(Position) * Sphere.Water.Planet.AverageRadius;
            SurfaceDistanceFromCenter = Math.Max(Sphere.Water.Planet.GetClosestSurfacePointLocal(ref surface).Length() + SURFACE_OFFSET, Sphere.Water.Planet.MinimumRadius);
            FluidHeight = NewFluidHeight = Math.Max(Sphere.Water.Radius - SurfaceDistanceFromCenter, 0);

            if (Depth < MIN_DEPTH)
                MinSplit(true);
        }

        /// <summary>
        /// Constructor for a child node
        /// </summary>
        public WaterTreeNode(WaterTreeNode parent, Vector3D position, Base6Directions.Direction direction, int depth, uint index, bool setupDefaultValue)
        {
            Sphere = parent.Sphere;
            Parent = parent;
            Position = position;
            Direction = direction;
            Depth = depth;
            Id = parent.Id;
            Id |= index << (2 * depth);
            Diameter = 1.0 / Math.Pow(2, Depth);

            Vector3 surface = Vector3.Normalize(Position) * Sphere.Water.Planet.AverageRadius;
            SurfaceDistanceFromCenter = Math.Max(Sphere.Water.Planet.GetClosestSurfacePointLocal(ref surface).Length() + SURFACE_OFFSET, Sphere.Water.Planet.MinimumRadius);

            if (setupDefaultValue)
            {
                FluidHeight = NewFluidHeight = Math.Max(Sphere.Water.Radius - SurfaceDistanceFromCenter, 0);
            }

            FluidHeight = NewFluidHeight;

            if (Depth < MIN_DEPTH)
                MinSplit(setupDefaultValue);
        }

        /// <summary>
        /// Gets the deepest node within the tree via Id
        /// </summary>
        public WaterTreeNode GetNode(ulong id)
        {
            ulong mask = (3ul << (Depth + 1) * 2); //3 = 0b11
            ulong index = (id & mask) >> (Depth + 1) * 2;

            if (Children == null)
            {
                if (Id != id)
                    throw new Exception();

                return this;
            }

            return Children[index].GetNode(id);
        }

        public WaterTreeNode GetNode(ref Vector3D position)
        {
            if (Children == null)
            {
                return this;
            }
            else
            {
                switch (Direction)
                {
                    case Base6Directions.Direction.Forward:
                        if (Position.X > position.X)
                        {
                            if (Position.Y > position.Y)
                            {
                                return Children[TOP_LEFT].GetNode(ref position);
                            }
                            else
                            {
                                return Children[BOTTOM_LEFT].GetNode(ref position);
                            }
                        }
                        else
                        {
                            if (Position.Y > position.Y)
                            {
                                return Children[TOP_RIGHT].GetNode(ref position);
                            }
                            else
                            {
                                return Children[BOTTOM_RIGHT].GetNode(ref position);
                            }
                        }
                    case Base6Directions.Direction.Backward:
                        if (Position.X < 0)
                        {
                            if (Position.Y > position.Y)
                            {
                                return Children[TOP_LEFT].GetNode(ref position);
                            }
                            else
                            {
                                return Children[BOTTOM_LEFT].GetNode(ref position);
                            }
                        }
                        else
                        {
                            if (Position.Y > position.Y)
                            {
                                return Children[TOP_RIGHT].GetNode(ref position);
                            }
                            else
                            {
                                return Children[BOTTOM_RIGHT].GetNode(ref position);
                            }
                        }
                    case Base6Directions.Direction.Right:
                        if (Position.Z < position.Z)
                        {
                            if (Position.Y > position.Y)
                            {
                                return Children[TOP_LEFT].GetNode(ref position);
                            }
                            else
                            {
                                return Children[BOTTOM_LEFT].GetNode(ref position);
                            }
                        }
                        else
                        {
                            if (Position.Y > position.Y)
                            {
                                return Children[TOP_RIGHT].GetNode(ref position);
                            }
                            else
                            {
                                return Children[BOTTOM_RIGHT].GetNode(ref position);
                            }
                        }
                    case Base6Directions.Direction.Left:
                        if (Position.Z < position.Z)
                        {
                            if (Position.Y > position.Y)
                            {
                                return Children[TOP_LEFT].GetNode(ref position);
                            }
                            else
                            {
                                return Children[BOTTOM_LEFT].GetNode(ref position);
                            }
                        }
                        else
                        {
                            if (Position.Y > position.Y)
                            {
                                return Children[TOP_RIGHT].GetNode(ref position);
                            }
                            else
                            {
                                return Children[BOTTOM_RIGHT].GetNode(ref position);
                            }
                        }
                    case Base6Directions.Direction.Up:
                        if (Position.X > position.X)
                        {
                            if (Position.Z > position.Z)
                            {
                                return Children[TOP_LEFT].GetNode(ref position);
                            }
                            else
                            {
                                return Children[BOTTOM_LEFT].GetNode(ref position);
                            }
                        }
                        else
                        {
                            if (Position.Z > position.Z)
                            {
                                return Children[TOP_RIGHT].GetNode(ref position);
                            }
                            else
                            {
                                return Children[BOTTOM_RIGHT].GetNode(ref position);
                            }
                        }
                    case Base6Directions.Direction.Down:
                        if (Position.X > position.X)
                        {
                            if (Position.Z < position.Z)
                            {
                                return Children[TOP_LEFT].GetNode(ref position);
                            }
                            else
                            {
                                return Children[BOTTOM_LEFT].GetNode(ref position);
                            }
                        }
                        else
                        {
                            if (Position.Z < position.Z)
                            {
                                return Children[TOP_RIGHT].GetNode(ref position);
                            }
                            else
                            {
                                return Children[BOTTOM_RIGHT].GetNode(ref position);
                            }
                        }
                    default:
                        throw new ArgumentException();
                }

                /*Vector3D offset = Position - position;

                switch (Direction)
                {
                    case Base6Directions.Direction.Forward:
                        if (offset.X > 0)
                        {
                            if (offset.Y > 0)
                            {
                                return Children[TOP_LEFT].GetNode(ref position);
                            }
                            else
                            {
                                return Children[BOTTOM_LEFT].GetNode(ref position);
                            }
                        }
                        else
                        {
                            if (offset.Y > 0)
                            {
                                return Children[TOP_RIGHT].GetNode(ref position);
                            }
                            else
                            {
                                return Children[BOTTOM_RIGHT].GetNode(ref position);
                            }
                        }
                    case Base6Directions.Direction.Backward:
                        if (offset.X < 0)
                        {
                            if (offset.Y > 0)
                            {
                                return Children[TOP_LEFT].GetNode(ref position);
                            }
                            else
                            {
                                return Children[BOTTOM_LEFT].GetNode(ref position);
                            }
                        }
                        else
                        {
                            if (offset.Y > 0)
                            {
                                return Children[TOP_RIGHT].GetNode(ref position);
                            }
                            else
                            {
                                return Children[BOTTOM_RIGHT].GetNode(ref position);
                            }
                        }
                    case Base6Directions.Direction.Right:
                        if (offset.Z < 0)
                        {
                            if (offset.Y > 0)
                            {
                                return Children[TOP_LEFT].GetNode(ref position);
                            }
                            else
                            {
                                return Children[BOTTOM_LEFT].GetNode(ref position);
                            }
                        }
                        else
                        {
                            if (offset.Y > 0)
                            {
                                return Children[TOP_RIGHT].GetNode(ref position);
                            }
                            else
                            {
                                return Children[BOTTOM_RIGHT].GetNode(ref position);
                            }
                        }
                    case Base6Directions.Direction.Left:
                        if (offset.Z < 0)
                        {
                            if (offset.Y > 0)
                            {
                                return Children[TOP_LEFT].GetNode(ref position);
                            }
                            else
                            {
                                return Children[BOTTOM_LEFT].GetNode(ref position);
                            }
                        }
                        else
                        {
                            if (offset.Y > 0)
                            {
                                return Children[TOP_RIGHT].GetNode(ref position);
                            }
                            else
                            {
                                return Children[BOTTOM_RIGHT].GetNode(ref position);
                            }
                        }
                    case Base6Directions.Direction.Up:
                        if (offset.X > 0)
                        {
                            if (offset.Z > 0)
                            {
                                return Children[TOP_LEFT].GetNode(ref position);
                            }
                            else
                            {
                                return Children[BOTTOM_LEFT].GetNode(ref position);
                            }
                        }
                        else
                        {
                            if (offset.Z > 0)
                            {
                                return Children[TOP_RIGHT].GetNode(ref position);
                            }
                            else
                            {
                                return Children[BOTTOM_RIGHT].GetNode(ref position);
                            }
                        }
                    case Base6Directions.Direction.Down:
                        if (offset.X > 0)
                        {
                            if (offset.Z < 0)
                            {
                                return Children[TOP_LEFT].GetNode(ref position);
                            }
                            else
                            {
                                return Children[BOTTOM_LEFT].GetNode(ref position);
                            }
                        }
                        else
                        {
                            if (offset.Z < 0)
                            {
                                return Children[TOP_RIGHT].GetNode(ref position);
                            }
                            else
                            {
                                return Children[BOTTOM_RIGHT].GetNode(ref position);
                            }
                        }
                    default:
                        throw new ArgumentException();
                }*/
            }
        }

        /// <summary>
        /// Splits the tree node, but does not run any fluid preservation.
        /// </summary>
        private void MinSplit(bool setupDefaultValue)
        {
            Children = new WaterTreeNode[4];

            int newDepth = Depth + 1;
            double halfDiameter = Diameter / 2;

            Vector3D right = Base6Directions.GetIntVector(Base6DirectionsUtils.GetRight(Direction)) * halfDiameter;
            Vector3D up = Base6Directions.GetIntVector(Base6DirectionsUtils.GetUp(Direction)) * halfDiameter;

            Children[TOP_LEFT] = new WaterTreeNode(this, Position - right - up, Direction, newDepth, TOP_LEFT, setupDefaultValue);
            Children[TOP_RIGHT] = new WaterTreeNode(this, Position + right - up, Direction, newDepth, TOP_RIGHT, setupDefaultValue);
            Children[BOTTOM_LEFT] = new WaterTreeNode(this, Position - right + up, Direction, newDepth, BOTTOM_LEFT, setupDefaultValue);
            Children[BOTTOM_RIGHT] = new WaterTreeNode(this, Position + right + up, Direction, newDepth, BOTTOM_RIGHT, setupDefaultValue);
        }

        /// <summary>
        /// Splits the tree node, preserving the correct fluid amount with regards to some children not having fluid at all
        /// </summary>
        public void Split()
        {
            MinSplit(false);

            if(FluidHeight > 0)
            {
                double maxRadius = SurfaceDistanceFromCenter + FluidHeight;

                //Naive method of preserving water volume. TODO, find a way to distritbute the fluid evenly and account for differing surface heights
                //Right now, the simulation itself will redsitribute fluid to be even

                //Perhaps distrbuting at first step with the lowest surface height, finding how much volume is left, then finally distributing evenly for the remaining volume.

                //todo
                /*double volume = GetVolume();
                double perVolume = volume / 4;

                double halfArea = Math.Pow(GetDiameter() / 2, 2);
                double perHeight = perVolume / halfArea;

                foreach (var child in Children)
                {
                    child.NewValue.FluidHeight = perHeight;
                }*/

                foreach (var child in Children)
                {
                    child.NewFluidHeight = FluidHeight;
                    child.FluidHeight = FluidHeight;
                }
            }
        }

        /// <summary>
        /// The opposite of <see cref="Split"/>. Removes all children and preserves the correct fluid amount
        /// </summary>
        public void Reduce()
        {
            double sumVolume = 0;
            SumVolume(this, ref sumVolume);

            Children = null;
            NewFluidHeight = sumVolume / (Diameter * Diameter);
        }

        /// <summary>
        /// Recursively calculates the total volume of fluid
        /// </summary>
        public static void SumVolume(WaterTreeNode tree, ref double sum)
        {
            if(tree.Children == null)
            {
                sum += tree.GetVolume();
            }
            else
            {
                foreach (var child in tree.Children)
                {
                    SumVolume(child, ref sum);
                }
            }
        }

        public double GetVolume()
        {
            if (FluidHeight == 0)
                return 0;

            return Diameter * Diameter * FluidHeight;
        }
    }
}
