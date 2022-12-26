using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace Jakaria.Utils
{
    public static class Base6DirectionsUtils
    {
        public static Base6Directions.Direction GetUp(Base6Directions.Direction direction)
        {
            switch (direction)
            {
                case Base6Directions.Direction.Forward:
                    return Base6Directions.Direction.Up;
                case Base6Directions.Direction.Backward:
                    return Base6Directions.Direction.Up;
                case Base6Directions.Direction.Up:
                    return Base6Directions.Direction.Backward;
                case Base6Directions.Direction.Down:
                    return Base6Directions.Direction.Forward;
                case Base6Directions.Direction.Right:
                    return Base6Directions.Direction.Up;
                case Base6Directions.Direction.Left:
                    return Base6Directions.Direction.Up;
                default:
                    throw new ArgumentException();
            }
        }

        public static Base6Directions.Direction GetRight(Base6Directions.Direction direction)
        {
            switch (direction)
            {
                case Base6Directions.Direction.Forward:
                    return Base6Directions.Direction.Right;
                case Base6Directions.Direction.Backward:
                    return Base6Directions.Direction.Left;
                case Base6Directions.Direction.Up:
                    return Base6Directions.Direction.Right;
                case Base6Directions.Direction.Down:
                    return Base6Directions.Direction.Right;
                case Base6Directions.Direction.Right:
                    return Base6Directions.Direction.Backward;
                case Base6Directions.Direction.Left:
                    return Base6Directions.Direction.Forward;
                default:
                    throw new ArgumentException();
            }
        }
    }
}
