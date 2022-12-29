using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace Jakaria.Utils
{
    /// <summary>
    /// A single side of the <see cref="SphereTree"/>
    /// </summary>
    public class QuadTree
    {
        public const int TOP_LEFT = 0;
        public const int TOP_RIGHT = 1;
        public const int BOTTOM_LEFT = 2;
        public const int BOTTOM_RIGHT = 3;

        public QuadTree[] Children;

        public SphereTree Sphere;

        /// <summary>
        /// The depth the node is within the tree
        /// </summary>
        public int Depth;

        /// <summary>
        /// The value stored on this node of the tree
        /// </summary>
        public WaterNode Value;

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
        public const int MAX_DEPTH = 15;

        /// <summary>
        /// The minimum depth the tree is allowed to be during creation
        /// </summary>
        public const int MIN_DEPTH = 8;

        /// <summary>
        /// The parent of this node, null when root
        /// </summary>
        public QuadTree Parent;

        /// <summary>
        /// Constructor for root node
        /// </summary>
        public QuadTree(SphereTree sphere, Base6Directions.Direction direction)
        {
            Sphere = sphere;
            Position = Base6Directions.GetVector(direction);
            Direction = direction;
            Id = 0;

            Vector3 surface = Vector3.Normalize(Position) * Sphere.Water.Planet.AverageRadius;
            Value.SurfaceDistanceFromCenter = Sphere.Water.Planet.GetClosestSurfacePointLocal(ref surface).Length() - 10;
            Value.FluidHeight = Math.Max(Sphere.Water.Radius - Value.SurfaceDistanceFromCenter, 10);

            Split();
        }

        /// <summary>
        /// Constructor for a child node
        /// </summary>
        public QuadTree(QuadTree parent, Vector3D position, Base6Directions.Direction direction, int depth, uint index)
        {
            Sphere = parent.Sphere;
            Parent = parent;
            Position = position;
            Direction = direction;
            Depth = depth;
            Id = parent.Id;
            Id |= index << (2 * depth);

            Vector3 surface = Vector3.Normalize(Position) * Sphere.Water.Planet.AverageRadius;
            Value.SurfaceDistanceFromCenter = Sphere.Water.Planet.GetClosestSurfacePointLocal(ref surface).Length() - 10;
            Value.FluidHeight = Math.Max(Sphere.Water.Radius - Value.SurfaceDistanceFromCenter, 10);

            if (Depth < MIN_DEPTH)
                Split();
        }

        /// <summary>
        /// Gets the deepest node within the tree via Id
        /// </summary>
        public QuadTree GetNode(ulong id)
        {
            ulong mask = ((ulong)3 << (Depth + 1) * 2); //3 = 0b11
            ulong index = (id & mask) >> (Depth + 1) * 2;

            if (Children == null)
            {
                if (Id != id)
                    throw new Exception();

                return this;
            }

            return Children[index].GetNode(id);
        }

        public void SetValue(Vector3D position, WaterNode value, int depth = MAX_DEPTH)
        {
            if (depth > Depth)
            {
                if (Children == null)
                {
                    Split();
                }

                GetChildNode(position).SetValue(position, value, depth);
            }

            Value = value;
        }

        public void InsertValue(Vector3D position, WaterNode value)
        {
            if (Depth >= MAX_DEPTH)
            {
                Value = value;
            }
            else if (Children == null)
            {
                Split();
                GetChildNode(position).SetValue(position, value, 0);
            }
            else
            {
                GetChildNode(position).InsertValue(position, value);
            }
        }

        public WaterNode GetValue(Vector3D position, int maxDepth = MAX_DEPTH)
        {
            if (Depth > maxDepth && Children != null)
            {
                QuadTree childNode = GetChildNode(position);
                if (childNode != null)
                {
                    return childNode.GetValue(position, maxDepth);
                }
            }

            return Value;
        }

        public QuadTree GetNode(Vector3D position, int maxDepth = MAX_DEPTH)
        {
            if (Depth > maxDepth && Children != null)
            {
                QuadTree childNode = GetChildNode(position);
                if (childNode != null)
                {
                    return childNode.GetNode(position, maxDepth);
                }
            }

            return this;
        }

        public void Split()
        {
            Children = new QuadTree[4];

            int newDepth = Depth + 1;
            double halfDiameter = GetDiameter() / 2;
            
            Vector3D right = Base6Directions.GetIntVector(Base6DirectionsUtils.GetRight(Direction)) * halfDiameter;
            Vector3D up = Base6Directions.GetIntVector(Base6DirectionsUtils.GetUp(Direction)) * halfDiameter;

            Children[TOP_LEFT] = new QuadTree(this, Position - right - up, Direction, newDepth, TOP_LEFT);
            Children[TOP_RIGHT] = new QuadTree(this, Position + right - up, Direction, newDepth, TOP_RIGHT);
            Children[BOTTOM_LEFT] = new QuadTree(this, Position - right + up, Direction, newDepth, BOTTOM_LEFT);
            Children[BOTTOM_RIGHT] = new QuadTree(this, Position + right + up, Direction, newDepth, BOTTOM_RIGHT);

            if(Value.FluidHeight > 0)
            {
                double maxRadius = Value.SurfaceDistanceFromCenter + Value.FluidHeight;

                foreach (var child in Children)
                {
                    Value.FluidHeight = Math.Max(maxRadius - child.Value.SurfaceDistanceFromCenter, 10);
                }
            }
        }

        public double GetDiameter()
        {
            return 1.0 / Math.Pow(2, Depth);
        }

        public double GetVolume()
        {
            double diamater = GetDiameter();
            return diamater * diamater * Value.FluidHeight;
        }

        public QuadTree GetChildNode(Vector3D position)
        {
            if (Children == null)
                return null;

            Vector3D offset = Position - position;

            switch (Direction)
            {
                case Base6Directions.Direction.Forward:
                    if (offset.X > 0)
                    {
                        if (offset.Y > 0)
                        {
                            return Children[TOP_LEFT];
                        }
                        else
                        {
                            return Children[BOTTOM_LEFT];
                        }
                    }
                    else
                    {
                        if (offset.Y > 0)
                        {
                            return Children[TOP_RIGHT];
                        }
                        else
                        {
                            return Children[BOTTOM_RIGHT];
                        }
                    }
                case Base6Directions.Direction.Backward:
                    if (offset.X < 0)
                    {
                        if (offset.Y > 0)
                        {
                            return Children[TOP_LEFT];
                        }
                        else
                        {
                            return Children[BOTTOM_LEFT];
                        }
                    }
                    else
                    {
                        if (offset.Y > 0)
                        {
                            return Children[TOP_RIGHT];
                        }
                        else
                        {
                            return Children[BOTTOM_RIGHT];
                        }
                    }
                case Base6Directions.Direction.Right:
                    if (offset.Z < 0)
                    {
                        if (offset.Y > 0)
                        {
                            return Children[TOP_LEFT];
                        }
                        else
                        {
                            return Children[BOTTOM_LEFT];
                        }
                    }
                    else
                    {
                        if (offset.Y > 0)
                        {
                            return Children[TOP_RIGHT];
                        }
                        else
                        {
                            return Children[BOTTOM_RIGHT];
                        }
                    }
                case Base6Directions.Direction.Left:
                    if (offset.Z > 0)
                    {
                        if (offset.Y > 0)
                        {
                            return Children[TOP_LEFT];
                        }
                        else
                        {
                            return Children[BOTTOM_LEFT];
                        }
                    }
                    else
                    {
                        if (offset.Y > 0)
                        {
                            return Children[TOP_RIGHT];
                        }
                        else
                        {
                            return Children[BOTTOM_RIGHT];
                        }
                    }
                case Base6Directions.Direction.Up:
                    if (offset.X > 0)
                    {
                        if (offset.Z < 0)
                        {
                            return Children[TOP_LEFT];
                        }
                        else
                        {
                            return Children[BOTTOM_LEFT];
                        }
                    }
                    else
                    {
                        if (offset.Z < 0)
                        {
                            return Children[TOP_RIGHT];
                        }
                        else
                        {
                            return Children[BOTTOM_RIGHT];
                        }
                    }
                case Base6Directions.Direction.Down:
                    if (offset.X > 0)
                    {
                        if (offset.Z > 0)
                        {
                            return Children[TOP_LEFT];
                        }
                        else
                        {
                            return Children[BOTTOM_LEFT];
                        }
                    }
                    else
                    {
                        if (offset.Z > 0)
                        {
                            return Children[TOP_RIGHT];
                        }
                        else
                        {
                            return Children[BOTTOM_RIGHT];
                        }
                    }
                default:
                    throw new ArgumentException();
            }
        }
    }
}
