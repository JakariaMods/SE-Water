using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace Jakaria.Utils
{
    public class SphereTree<T>
    {
        public readonly QuadTree<T> Forward = new QuadTree<T>(Base6Directions.Direction.Forward);
        public readonly QuadTree<T> Back = new QuadTree<T>(Base6Directions.Direction.Backward);
        public readonly QuadTree<T> Right = new QuadTree<T>(Base6Directions.Direction.Right);
        public readonly QuadTree<T> Left = new QuadTree<T>(Base6Directions.Direction.Left);
        public readonly QuadTree<T> Up = new QuadTree<T>(Base6Directions.Direction.Up);
        public readonly QuadTree<T> Down = new QuadTree<T>(Base6Directions.Direction.Down);

        public IEnumerable<QuadTree<T>> GetFaces()
        {
            yield return Forward;
            yield return Back;
            yield return Right;
            yield return Left;
            yield return Up;
            yield return Down;
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

        public const int MAX_DEPTH = QuadTree<T>.MAX_DEPTH;

        public T GetValueGlobal(Vector3 worldNormal, int maxDepth = MAX_DEPTH)
        {
            Vector3 localNormal = Vector3.Transform(worldNormal, TransformInv);

            return GetValueLocal(localNormal, maxDepth);
        }

        public T GetValueLocal(Vector3 localNormal, int maxDepth = MAX_DEPTH)
        {
            localNormal = Vector3Extensions.ProjectOntoUnitCube(localNormal);
            return GetFaceLocal(localNormal).GetValue(localNormal, maxDepth);
        }

        public void InsertValueGlobal(Vector3 worldNormal, T value)
        {
            Vector3 localNormal = Vector3.Transform(worldNormal, TransformInv);
            InsertValueLocal(localNormal, value);
        }

        public void InsertValueLocal(Vector3 localNormal, T value)
        {
            localNormal = Vector3Extensions.ProjectOntoUnitCube(localNormal);
            GetFaceLocal(localNormal).InsertValue(localNormal, value);
        }

        public void SetValueGlobal(Vector3 worldNormal, T value, int depth = MAX_DEPTH)
        {
            Vector3 localNormal = Vector3.Transform(worldNormal, TransformInv);
            SetValueLocal(localNormal, value, depth);
        }

        public void SetValueLocal(Vector3 localNormal, T value, int depth = MAX_DEPTH)
        {
            localNormal = Vector3Extensions.ProjectOntoUnitCube(localNormal);
            GetFaceLocal(localNormal).SetValue(localNormal, value, depth);
        }

        public void SetValue(Vector3 position, Base6Directions.Direction direction, T value, int depth = MAX_DEPTH)
        {
            GetFace(direction).SetValue(position, value, depth);
        }

        public QuadTree<T> GetFace(Base6Directions.Direction direction)
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

        public QuadTree<T> GetFaceGlobal(Vector3 worldNormal)
        {
            Vector3 localNormal = Vector3.Transform(worldNormal, TransformInv);
            return GetFaceLocal(localNormal);
        }

        public QuadTree<T> GetFaceLocal(Vector3 localNormal)
        {
            localNormal = Vector3Extensions.ProjectOntoUnitCube(localNormal);
            Vector3 dominantDirection = localNormal.MaxComponentSign();

            if (dominantDirection.X < 0)
                return Left;
            if (dominantDirection.X > 0)
                return Right;

            if (dominantDirection.Y < 0)
                return Down;
            if (dominantDirection.Y > 0)
                return Up;

            if (dominantDirection.Z > 0)
                return Forward;
            if (dominantDirection.Z < 0)
                return Back;

            throw new ArgumentException();
        }

        public void GetNeighbors(QuadTree<T> tree, List<QuadTree<T>> neighbors) 
        {
            QuadTree<T> neighbor;

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

        private QuadTree<T> GetNeighborOfGreaterOrEqualSize(QuadTree<T> tree, Direction direction)
        {
            if (tree.Parent == null)
            {
                switch (direction)
                {
                    case Direction.Top:
                        if (tree.Normal == Base6Directions.Direction.Forward ||
                            tree.Normal == Base6Directions.Direction.Right ||
                            tree.Normal == Base6Directions.Direction.Backward ||
                            tree.Normal == Base6Directions.Direction.Left)
                            return GetFace(Base6Directions.Direction.Up);
                        else if (tree.Normal == Base6Directions.Direction.Up)
                            return GetFace(Base6Directions.Direction.Backward); //TODO VERIFY
                        else
                            return GetFace(Base6Directions.Direction.Up); //TODO VERIFY
                    case Direction.Bottom:
                        if (tree.Normal == Base6Directions.Direction.Forward ||
                            tree.Normal == Base6Directions.Direction.Right ||
                            tree.Normal == Base6Directions.Direction.Backward ||
                            tree.Normal == Base6Directions.Direction.Left)
                            return GetFace(Base6Directions.Direction.Down);
                        else if (tree.Normal == Base6Directions.Direction.Up)
                            return GetFace(Base6Directions.Direction.Forward); //TODO VERIFY
                        else
                            return GetFace(Base6Directions.Direction.Backward); //TODO VERIFY
                    case Direction.Left:
                        if (tree.Normal == Base6Directions.Direction.Forward)
                            return GetFace(Base6Directions.Direction.Left);
                        else if (tree.Normal == Base6Directions.Direction.Left)
                            return GetFace(Base6Directions.Direction.Backward);
                        else if (tree.Normal == Base6Directions.Direction.Backward)
                            return GetFace(Base6Directions.Direction.Right);
                        else if (tree.Normal == Base6Directions.Direction.Right)
                            return GetFace(Base6Directions.Direction.Forward);
                        else if (tree.Normal == Base6Directions.Direction.Up)
                            return GetFace(Base6Directions.Direction.Left);
                        else
                            return GetFace(Base6Directions.Direction.Right);
                    case Direction.Right:
                        if (tree.Normal == Base6Directions.Direction.Forward)
                            return GetFace(Base6Directions.Direction.Right);
                        else if (tree.Normal == Base6Directions.Direction.Left)
                            return GetFace(Base6Directions.Direction.Forward);
                        else if (tree.Normal == Base6Directions.Direction.Backward)
                            return GetFace(Base6Directions.Direction.Left);
                        else if (tree.Normal == Base6Directions.Direction.Right)
                            return GetFace(Base6Directions.Direction.Backward);
                        else if (tree.Normal == Base6Directions.Direction.Up)
                            return GetFace(Base6Directions.Direction.Right);
                        else
                            return GetFace(Base6Directions.Direction.Left);
                    default:
                        throw new Exception();
                }
            }

            ulong mask = (ulong)3 << tree.Depth * 2; //0b11
            ulong index = (tree.Id & mask) >> tree.Depth * 2;
            QuadTree<T> node;

            switch (direction)
            {
                case Direction.Top:
                    if (index == QuadTree<T>.BOTTOM_LEFT)
                        return tree.Parent.Children[QuadTree<T>.TOP_LEFT];
                    else if (index == QuadTree<T>.BOTTOM_RIGHT)
                        return tree.Parent.Children[QuadTree<T>.TOP_RIGHT];

                    node = GetNeighborOfGreaterOrEqualSize(tree.Parent, direction);
                    if (node == null)
                        return null;
                    if (node.Children == null)
                        return node;

                    if (index == QuadTree<T>.TOP_LEFT)
                        return node.Children[QuadTree<T>.BOTTOM_LEFT];
                    else
                        return node.Children[QuadTree<T>.BOTTOM_RIGHT];
                case Direction.Bottom:
                    if (index == QuadTree<T>.TOP_LEFT)
                        return tree.Parent.Children[QuadTree<T>.BOTTOM_LEFT];
                    else if (index == QuadTree<T>.TOP_RIGHT)
                        return tree.Parent.Children[QuadTree<T>.BOTTOM_RIGHT];

                    node = GetNeighborOfGreaterOrEqualSize(tree.Parent, direction);
                    if (node == null)
                        return null;
                    if (node.Children == null)
                        return node;

                    if (index == QuadTree<T>.BOTTOM_LEFT)
                        return node.Children[QuadTree<T>.TOP_LEFT];
                    else
                        return node.Children[QuadTree<T>.TOP_RIGHT];
                case Direction.Left:
                    if (index == QuadTree<T>.TOP_RIGHT)
                        return tree.Parent.Children[QuadTree<T>.TOP_LEFT];
                    else if (index == QuadTree<T>.BOTTOM_RIGHT)
                        return tree.Parent.Children[QuadTree<T>.BOTTOM_LEFT];

                    node = GetNeighborOfGreaterOrEqualSize(tree.Parent, direction);
                    if (node == null)
                        return null;
                    if (node.Children == null)
                        return node;

                    if (index == QuadTree<T>.TOP_LEFT)
                        return node.Children[QuadTree<T>.TOP_RIGHT];
                    else
                        return node.Children[QuadTree<T>.BOTTOM_RIGHT];
                case Direction.Right:
                    if (index == QuadTree<T>.TOP_LEFT)
                        return tree.Parent.Children[QuadTree<T>.TOP_RIGHT];
                    else if (index == QuadTree<T>.BOTTOM_LEFT)
                        return tree.Parent.Children[QuadTree<T>.BOTTOM_RIGHT];

                    node = GetNeighborOfGreaterOrEqualSize(tree.Parent, direction);
                    if (node == null)
                        return null;
                    if (node.Children == null)
                        return node;

                    if (index == QuadTree<T>.TOP_RIGHT)
                        return node.Children[QuadTree<T>.TOP_LEFT];
                    else
                        return node.Children[QuadTree<T>.BOTTOM_LEFT];
                default:
                    throw new Exception();
            }
        }

        private void GetNeighborsOfSmallerSize(QuadTree<T> neighbor, List<QuadTree<T>> neighbors, Direction direction)
        {
            if (neighbor.Children == null)
            {
                neighbors.Add(neighbor);
            }
            else
            {
                switch (direction)
                {
                    case Direction.Top:
                        GetNeighborsOfSmallerSize(neighbor.Children[QuadTree<T>.BOTTOM_LEFT], neighbors, direction);
                        GetNeighborsOfSmallerSize(neighbor.Children[QuadTree<T>.BOTTOM_RIGHT], neighbors, direction);
                        break;
                    case Direction.Bottom:
                        GetNeighborsOfSmallerSize(neighbor.Children[QuadTree<T>.TOP_LEFT], neighbors, direction);
                        GetNeighborsOfSmallerSize(neighbor.Children[QuadTree<T>.TOP_RIGHT], neighbors, direction);
                        break;
                    case Direction.Left:
                        GetNeighborsOfSmallerSize(neighbor.Children[QuadTree<T>.TOP_RIGHT], neighbors, direction);
                        GetNeighborsOfSmallerSize(neighbor.Children[QuadTree<T>.BOTTOM_RIGHT], neighbors, direction);
                        break;
                    case Direction.Right:
                        GetNeighborsOfSmallerSize(neighbor.Children[QuadTree<T>.TOP_LEFT], neighbors, direction);
                        GetNeighborsOfSmallerSize(neighbor.Children[QuadTree<T>.BOTTOM_LEFT], neighbors, direction);
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
        public void CopyTo(SphereTree<T> otherTree)
        {
            foreach (var face in GetFaces())
            {
                CopyTree(face, otherTree.GetFace(face.Normal));
            }
        }

        /// <summary>
        /// <see cref="SphereTree{T}.CopyTo(SphereTree{T})"/>
        /// </summary>
        private void CopyTree(QuadTree<T> tree, QuadTree<T> otherTree)
        {
            if (tree.HasValue)
            {
                otherTree.Value = tree.Value;
                otherTree.HasValue = true;
            }
            else
            {
                otherTree.Value = default(T);
                otherTree.HasValue = false;
            }

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

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            builder.Append(PrintTree(Forward, nameof(Forward)));
            builder.Append(PrintTree(Back, nameof(Back)));
            builder.Append(PrintTree(Up, nameof(Up)));
            builder.Append(PrintTree(Down, nameof(Down)));
            builder.Append(PrintTree(Left, nameof(Left)));
            builder.Append(PrintTree(Right, nameof(Right)));

            return builder.ToString();
        }

        /// <summary>
        /// <see cref="SphereTree{T}.ToString"/>
        /// </summary>
        private StringBuilder PrintTree(QuadTree<T> tree, string name)
        {
            StringBuilder builder = new StringBuilder();

            string text;

            if (tree.Children == null)
                text = $"{name.PadRight(6)}: TreeDepth: {tree.Depth}, Id: {Convert.ToString((int)tree.Id, 2).PadLeft(Math.Min(sizeof(ulong), (QuadTree<T>.MAX_DEPTH + 1) * 2), '0')}, Value: {tree.Value.ToString().PadRight(6)}";
            else
                text = $"{name.PadRight(6)}: TreeDepth: {tree.Depth}, Id: {Convert.ToString((int)tree.Id, 2).PadLeft(Math.Min(sizeof(ulong), (QuadTree<T>.MAX_DEPTH + 1) * 2), '0')}";

            builder.AppendLine(text.PadLeft(text.Length + (tree.Depth * 2)));

            if (tree.Children != null)
            {
                for (int i = 0; i < tree.Children.Length; i++)
                {
                    builder.Append(PrintTree(tree.Children[i], i.ToString()));
                }
            }

            return builder;
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
