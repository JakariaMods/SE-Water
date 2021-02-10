using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace Jakaria.Utils
{
    public class Quadtree<T>
    {
        public Quadtree<T>[] corners { private set; get; }
        public bool isSplit { private set; get; } = false;
        public bool hasData { private set; get; } = false;
        public T data { private set; get; }

        public Vector2D position { private set; get; }
        public Vector2D size { private set; get; }

        public Quadtree(Vector2D position, Vector2D size)
        {
            this.position = position;
            this.size = size;
        }

        public bool Split()
        {
            if (isSplit)
            {
                return false;
            }
            else
            {
                isSplit = true;
                corners = new Quadtree<T>[4];
                corners[0] = new Quadtree<T>(position + new Vector2D(), size / 2);
                corners[1] = new Quadtree<T>(position + new Vector2D(), size / 2);
                corners[2] = new Quadtree<T>(position + new Vector2D(), size / 2);
                corners[3] = new Quadtree<T>(position + new Vector2D(), size / 2);

                return true;
            }
        }

        public Quadtree<T> GetCorner(Vector2D position)
        {
            if (isSplit)
            {
                if (position.X < this.position.X)
                {
                    if (position.Y < this.position.Y)
                    {
                        return corners[(int)Corner.TopLeft];
                    }
                    else
                    {
                        return corners[(int)Corner.BottomLeft];
                    }
                }
                else
                {
                    if (position.Y < this.position.Y)
                    {
                        return corners[(int)Corner.TopRight];
                    }
                    else
                    {
                        return corners[(int)Corner.BottomRight];
                    }
                }
            }
            else
                return this;
        }

        public enum Corner : uint
        {
            TopLeft = 0,
            TopRight = 1,
            BottomLeft = 2,
            BottomRight = 3,
        }

        public void Insert(T data, Vector2D position)
        {

            if (isSplit)
            {
                GetCorner(position).Insert(data, position);
            }
            else
            {
                this.data = data;
                hasData = true;
            }
        }

        public void Clear()
        {
            corners = null;
            hasData = false;
            isSplit = false;
            data = default(T);
        }
    }
}
