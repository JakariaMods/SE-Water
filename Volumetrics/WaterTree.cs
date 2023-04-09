using Jakaria.Components;
using Jakaria.Utils;
using Sandbox.Game.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace Jakaria.Volumetrics
{
    /// <summary>
    /// Spherical quad-tree-like structure for volumetric water simulation
    /// </summary>
    public class WaterTree
    {
        public WaterComponent Water;

        public readonly WaterTreeNode Forward;
        public readonly WaterTreeNode Back;
        public readonly WaterTreeNode Right;
        public readonly WaterTreeNode Left;
        public readonly WaterTreeNode Up;
        public readonly WaterTreeNode Down;

        public WaterTreeNode[] Faces;

        public WaterTree(WaterComponent water)
        {
            Water = water;

            Forward = new WaterTreeNode(this, Base6Directions.Direction.Forward);
            Back = new WaterTreeNode(this, Base6Directions.Direction.Backward);
            Right = new WaterTreeNode(this, Base6Directions.Direction.Right);
            Left = new WaterTreeNode(this, Base6Directions.Direction.Left);
            Up = new WaterTreeNode(this, Base6Directions.Direction.Up);
            Down = new WaterTreeNode(this, Base6Directions.Direction.Down);

            Faces = new WaterTreeNode[6]
            {
                Forward,
                Back,
                Right,
                Left,
                Up,
                Down
            };
        }

        public MatrixD Transform
        {
            get
            {
                return _transform;
            }
            set
            {
                _transform = value;
                MatrixD.Invert(ref _transform, out TransformInv);
            }
        }

        public MatrixD TransformInv = MatrixD.Identity;

        private MatrixD _transform;

        public const int MAX_DEPTH = WaterTreeNode.MAX_DEPTH;

        public WaterTreeNode GetNodeGlobal(Vector3D worldNormal)
        {
            Vector3D localNormal = Vector3D.Transform(worldNormal, TransformInv);
            return GetNodeLocal(localNormal);
        }

        public WaterTreeNode GetNodeLocal(Vector3D localNormal)
        {
            localNormal = Vector3DExtensions.ProjectOntoUnitCube(localNormal);
            return GetFaceLocal(localNormal).GetNode(ref localNormal);
        }

        public WaterTreeNode GetFace(Base6Directions.Direction direction)
        {
            switch (direction)
            {
                case Base6Directions.Direction.Forward:
                    return Forward;
                case Base6Directions.Direction.Backward:
                    return Back;
                case Base6Directions.Direction.Up:
                    return Up;
                case Base6Directions.Direction.Down:
                    return Down;
                case Base6Directions.Direction.Right:
                    return Right;
                case Base6Directions.Direction.Left:
                    return Left;
                default:
                    throw new ArgumentException();
            }
        }

        public WaterTreeNode GetFaceGlobal(Vector3D worldNormal)
        {
            Vector3D localNormal = Vector3D.Transform(worldNormal, TransformInv);
            return GetFaceLocal(localNormal);
        }

        public WaterTreeNode GetFaceLocal(Vector3D localNormal)
        {
            //TODO TRY OUT GetDirection?
            Base6Directions.Direction direction = Base6Directions.GetClosestDirection((Vector3)localNormal);
            switch (direction)
            {
                case Base6Directions.Direction.Forward:
                    return Forward;
                case Base6Directions.Direction.Backward:
                    return Back;
                case Base6Directions.Direction.Left:
                    return Left;
                case Base6Directions.Direction.Right:
                    return Right;
                case Base6Directions.Direction.Up:
                    return Up;
                case Base6Directions.Direction.Down:
                    return Down;
                default:
                    throw new ArgumentException();
            }
        }

        public void GetNeighbors(WaterTreeNode tree, List<WaterTreeNode> neighbors) 
        {
            WaterTreeNode neighbor;

            neighbor = GetNeighborOfGreaterOrEqualSize(tree, Direction.Top);
            if (neighbor != null)
                GetNeighborsOfSmallerSize(neighbor, neighbors, Direction.Top);

            neighbor = GetNeighborOfGreaterOrEqualSize(tree, Direction.Bottom);
            if (neighbor != null)
                GetNeighborsOfSmallerSize(neighbor, neighbors, Direction.Bottom);

            neighbor = GetNeighborOfGreaterOrEqualSize(tree, Direction.Left);
            if (neighbor != null)
                GetNeighborsOfSmallerSize(neighbor, neighbors, Direction.Left);

            neighbor = GetNeighborOfGreaterOrEqualSize(tree, Direction.Right);
            if (neighbor != null)
                GetNeighborsOfSmallerSize(neighbor, neighbors, Direction.Right);
        }

        private WaterTreeNode GetNeighborOfGreaterOrEqualSize(WaterTreeNode tree, Direction direction)
        {
            int depth = tree.Depth << 1; //Multiply by two
            ulong mask = 3ul << depth; //0b11
            ulong index = (tree.Id & mask) >> depth;
            WaterTreeNode node;

            if(tree.Parent == null)
                return null;
            
            switch (direction)
            {
                case Direction.Top:
                    if (index == WaterTreeNode.BOTTOM_LEFT)
                        return tree.Parent.Children[WaterTreeNode.TOP_LEFT];
                    else if (index == WaterTreeNode.BOTTOM_RIGHT)
                        return tree.Parent.Children[WaterTreeNode.TOP_RIGHT];

                    node = GetNeighborOfGreaterOrEqualSize(tree.Parent, direction);
                    if (node == null)
                        return null;
                    if (node.Children == null)
                        return node;
                    if (index == WaterTreeNode.TOP_LEFT)
                        return node.Children[WaterTreeNode.BOTTOM_LEFT];

                    return node.Children[WaterTreeNode.BOTTOM_RIGHT];
                case Direction.Bottom:
                    if (index == WaterTreeNode.TOP_LEFT)
                        return tree.Parent.Children[WaterTreeNode.BOTTOM_LEFT];
                    else if (index == WaterTreeNode.TOP_RIGHT)
                        return tree.Parent.Children[WaterTreeNode.BOTTOM_RIGHT];

                    node = GetNeighborOfGreaterOrEqualSize(tree.Parent, direction);
                    if (node == null)
                        return null;
                    if (node.Children == null)
                        return node;
                    if (index == WaterTreeNode.BOTTOM_LEFT)
                        return node.Children[WaterTreeNode.TOP_LEFT];

                    return node.Children[WaterTreeNode.TOP_RIGHT];
                case Direction.Left:
                    if (index == WaterTreeNode.TOP_RIGHT)
                        return tree.Parent.Children[WaterTreeNode.TOP_LEFT];
                    else if (index == WaterTreeNode.BOTTOM_RIGHT)
                        return tree.Parent.Children[WaterTreeNode.BOTTOM_LEFT];

                    node = GetNeighborOfGreaterOrEqualSize(tree.Parent, direction);
                    if (node == null)
                        return null;
                    if (node.Children == null)
                        return node;
                    if (index == WaterTreeNode.TOP_LEFT)
                        return node.Children[WaterTreeNode.TOP_RIGHT];

                    return node.Children[WaterTreeNode.BOTTOM_RIGHT];
                case Direction.Right:
                    if (index == WaterTreeNode.TOP_LEFT)
                        return tree.Parent.Children[WaterTreeNode.TOP_RIGHT];
                    else if (index == WaterTreeNode.BOTTOM_LEFT)
                        return tree.Parent.Children[WaterTreeNode.BOTTOM_RIGHT];

                    node = GetNeighborOfGreaterOrEqualSize(tree.Parent, direction);
                    if (node == null)
                        return null;
                    if (node.Children == null)
                        return node;
                    if (index == WaterTreeNode.TOP_RIGHT)
                        return node.Children[WaterTreeNode.TOP_LEFT];

                    return node.Children[WaterTreeNode.BOTTOM_LEFT];
                default:
                    throw new Exception();
            }
            
        }

        private void GetNeighborsOfSmallerSize(WaterTreeNode neighbor, List<WaterTreeNode> neighbors, Direction direction)
        {
            if (neighbor.Children == null)
            {
                if(neighbor.Simulated)
                    neighbors.Add(neighbor);
            }
            else
            {
                switch (direction)
                {
                    case Direction.Top:
                        GetNeighborsOfSmallerSize(neighbor.Children[WaterTreeNode.BOTTOM_LEFT], neighbors, direction);
                        GetNeighborsOfSmallerSize(neighbor.Children[WaterTreeNode.BOTTOM_RIGHT], neighbors, direction);
                        break;
                    case Direction.Bottom:
                        GetNeighborsOfSmallerSize(neighbor.Children[WaterTreeNode.TOP_LEFT], neighbors, direction);
                        GetNeighborsOfSmallerSize(neighbor.Children[WaterTreeNode.TOP_RIGHT], neighbors, direction);
                        break;
                    case Direction.Left:
                        GetNeighborsOfSmallerSize(neighbor.Children[WaterTreeNode.TOP_RIGHT], neighbors, direction);
                        GetNeighborsOfSmallerSize(neighbor.Children[WaterTreeNode.BOTTOM_RIGHT], neighbors, direction);
                        break;
                    case Direction.Right:
                        GetNeighborsOfSmallerSize(neighbor.Children[WaterTreeNode.TOP_LEFT], neighbors, direction);
                        GetNeighborsOfSmallerSize(neighbor.Children[WaterTreeNode.BOTTOM_LEFT], neighbors, direction);
                        break;
                    default:
                        throw new Exception();
                }
            }
        }

        /// <summary>
        /// Copies the values of this tree to the other tree. Does not create new value objects for classes
        /// </summary>
        /// <param name="otherTree"></param>
        public void CopyTo(WaterTree otherTree)
        {
            foreach (var face in Faces)
            {
                CopyTree(face, otherTree.GetFace(face.Direction));
            }
        }

        /// <summary>
        /// Copies values and structure from one tree to another <see cref="WaterTree{T}.CopyTo(WaterTree{T})"/>
        /// </summary>
        private void CopyTree(WaterTreeNode tree, WaterTreeNode otherTree)
        {
            otherTree.FluidHeight = tree.FluidHeight;
            otherTree.NewFluidHeight = tree.NewFluidHeight;
            otherTree.SurfaceDistanceFromCenter = tree.SurfaceDistanceFromCenter;

            if (tree.Children == null)
            {
                if (otherTree.Children != null)
                {
                    otherTree.Children = null;
                }
            }
            else
            {
                if (otherTree.Children == null)
                {
                    otherTree.Split();
                }

                CopyTree(tree.Children[0], otherTree.Children[0]);
                CopyTree(tree.Children[1], otherTree.Children[1]);
                CopyTree(tree.Children[2], otherTree.Children[2]);
                CopyTree(tree.Children[3], otherTree.Children[3]);
            }
        }
    }

    public enum Direction
    {
        Top,
        Bottom,
        Left,
        Right
    }
}
