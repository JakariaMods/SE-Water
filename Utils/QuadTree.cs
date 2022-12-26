using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace Jakaria.Utils
{
    public class QuadTree<T>
    {
        public const int TOP_LEFT = 0;
        public const int TOP_RIGHT = 1;
        public const int BOTTOM_LEFT = 2;
        public const int BOTTOM_RIGHT = 3;

        public QuadTree<T>[] Children;

        /// <summary>
        /// The depth the node is within the tree
        /// </summary>
        public int Depth;

        /// <summary>
        /// The value stored on this node of the tree
        /// </summary>
        public T Value;

        /// <summary>
        /// True if any value has intentionally been given to <see cref="Value"/>
        /// </summary>
        public bool HasValue;

        /// <summary>
        /// The position on the unit cube's face
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// The cube-face direction
        /// </summary>
        public Base6Directions.Direction Normal;

        /// <summary>
        /// The ID of the face within the tree, denoting how it can be traversed to locate it
        /// </summary>
        public ulong Id;

        /// <summary>
        /// The maximum split depth of the <see cref="QuadTree{T}"/>
        /// </summary>
        public const int MAX_DEPTH = 15;

        /// <summary>
        /// The parent of this node, null when root
        /// </summary>
        public QuadTree<T> Parent;

        /// <summary>
        /// Constructor for root node
        /// </summary>
        public QuadTree(Base6Directions.Direction normal)
        {
            Position = Base6Directions.GetVector(normal);
            Normal = normal;
            Id = 0;
        }

        /// <summary>
        /// Constructor for child node
        /// </summary>
        public QuadTree(QuadTree<T> parent, Vector3 position, Base6Directions.Direction normal, int depth, uint index)
        {
            Parent = parent;
            Position = position;
            Normal = normal;
            Depth = depth;
            Id = parent.Id;
            Id |= index << (2 * depth);
        }

        /// <summary>
        /// Gets the deepest node within the tree via Id
        /// </summary>
        public QuadTree<T> GetNode(ulong id)
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

        public void SetValue(Vector3 position, T value, int depth = MAX_DEPTH)
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
            HasValue = true;
        }

        public void InsertValue(Vector3 position, T value)
        {
            if (!HasValue || Depth == MAX_DEPTH)
            {
                Value = value;
                HasValue = true;
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

        public T GetValue(Vector3 position, int maxDepth = MAX_DEPTH)
        {
            if (Depth > maxDepth && Children != null)
            {
                QuadTree<T> childNode = GetChildNode(position);
                if (childNode != null)
                {
                    return childNode.GetValue(position, maxDepth);
                }
            }

            return Value;
        }

        public QuadTree<T> GetNode(Vector3 position, int maxDepth = MAX_DEPTH)
        {
            if (Depth > maxDepth && Children != null)
            {
                QuadTree<T> childNode = GetChildNode(position);
                if (childNode != null)
                {
                    return childNode.GetNode(position, maxDepth);
                }
            }

            return this;
        }

        public void Split()
        {
            Children = new QuadTree<T>[4];

            int newDepth = Depth + 1;
            float size = GetDiameter() / 2;
            
            Vector3 right = Base6Directions.GetVector(Base6DirectionsUtils.GetRight(Normal)) * size;
            Vector3 up = Base6Directions.GetVector(Base6DirectionsUtils.GetUp(Normal)) * size;

            Children[TOP_LEFT] = new QuadTree<T>(this, Position - right - up, Normal, newDepth, TOP_LEFT);
            Children[TOP_RIGHT] = new QuadTree<T>(this, Position + right - up, Normal, newDepth, TOP_RIGHT);
            Children[BOTTOM_LEFT] = new QuadTree<T>(this, Position - right + up, Normal, newDepth, BOTTOM_LEFT);
            Children[BOTTOM_RIGHT] = new QuadTree<T>(this, Position + right + up, Normal, newDepth, BOTTOM_RIGHT);
        }

        public float GetDiameter()
        {
            return 1f / (float)Math.Pow(2, Depth);
        }

        public QuadTree<T> GetChildNode(Vector3 position)
        {
            if (Children == null)
                return null;

            Vector3 offset = Position - position;

            switch (Normal)
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
