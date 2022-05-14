using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace Jakaria.Utils
{
    public class SphereTree<T>
    {
        public const int NODES_COUNT = 8;

        public Vector3D CenterPosition;
        public double Radius;
        public int MaxDepth;

        private TreeNode<T>[] _nodes;
        private double _sqrRadius;

        public SphereTree(Vector3D centerPosition, double radius, int minNodeDepth, int maxNodeDepth)
        {
            CenterPosition = centerPosition;
            Radius = radius;
            MaxDepth = maxNodeDepth;

            _nodes = new TreeNode<T>[NODES_COUNT];
            _sqrRadius = Radius * Radius;

            for (int i = 0; i < NODES_COUNT; i++)
            {
                _nodes[i] = new TreeNode<T>(radius);
            }
        }

        public void Set(Vector3D position, T data, int nodeDepth)
        {
            if (Vector3D.DistanceSquared(position, CenterPosition) > _sqrRadius)
                return;
        }
    }

    public class TreeNode<T>
    {
        public double Radius;
        public T Value;
        
        private TreeNode<T>[] _nodes;

        public TreeNode(double radius)
        {
            Radius = radius;
        }

        public void Split()
        {
            if (_nodes == null)
            {
                _nodes = new TreeNode<T>[SphereTree<T>.NODES_COUNT];

                for (int i = 0; i < SphereTree<T>.NODES_COUNT; i++)
                {
                    _nodes[i] = new TreeNode<T>(Radius);
                }
            }
        }
    }
}
