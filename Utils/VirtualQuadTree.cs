using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jakaria.Utils
{
    /// <summary>
    /// Version of QuadTree that destroys intermediary nodes for memory efficiency. Only leaf nodes exist
    /// </summary>
    public class VirtualQuadTree
    {
        public const int MAX_DEPTH = 12;

        private Dictionary<ulong, TreeNode> _node = new Dictionary<ulong, TreeNode>();

        /// <summary>
        /// Get a node at a position given an ID
        /// </summary>
        public TreeNode GetNode(ulong id)
        {
            for (int i = 0; i < MAX_DEPTH; i += 2)
            {
                ulong mask = ~(1ul << sizeof(ulong) - i);
                id = id & mask; //this is wrong
                
                TreeNode node;
                if (_node.TryGetValue(mask, out node))
                    return node;
            }

            throw new Exception();
        }
    }

    public class TreeNode
    {
        public const int TOP_LEFT = 0;
        public const int TOP_RIGHT = 1;
        public const int BOTTOM_LEFT = 2;
        public const int BOTTOM_RIGHT = 3;

        private int _id;

        public override int GetHashCode()
        {
            return _id.GetHashCode();
        }
    }
}
