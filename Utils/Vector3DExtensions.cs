using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace Jakaria.Utils
{
    /// <summary>
    /// Extension methods for <see cref="Vector3D"/>
    /// </summary>
    public static class Vector3DExtensions
    {
        /// <summary>
        /// Returns the sign of the vector with the greatest distance from zero. 
        /// EX: {-6, 5, 8} => {0, 0, 1 }
        /// EX: {-6, 5, 4} => {-1, 0, 0}
        /// </summary>
        public static Vector3D MaxComponentSign(this Vector3D vector)
        {
            return vector * vector.MaxComponent();
        }
        
        /// <summary>
        /// Returns the vector with the greatest distance from zero
        /// </summary>
        public static Vector3D MaxComponent(this Vector3D vector)
        {
            Vector3D abs = Vector3D.Abs(vector);

            if (abs.X > abs.Y && abs.X > abs.Z)
                return Vector3D.UnitX;

            if (abs.Y > abs.X && abs.Y > abs.Z)
                return Vector3D.UnitY;

            if (abs.Z > abs.X && abs.Z > abs.Y)
                return Vector3D.UnitZ;

            return Vector3D.Zero;
        }

        /// <summary>
        /// Gets the sign of each vector
        /// </summary>
        public static Vector3D Sign(this Vector3D vector)
        {
            return new Vector3D
            {
                X = Math.Sign(vector.X),
                Y = Math.Sign(vector.Y),
                Z = Math.Sign(vector.Z),
            };
        }

        /// <summary>
        /// Gets the sign of each vector and lets any zero value be positive
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static Vector3D SignNonZero(this Vector3D vector)
        {
            Vector3D signs = new Vector3D
            {
                X = Math.Sign(vector.X),
                Y = Math.Sign(vector.Y),
                Z = Math.Sign(vector.Z),
            };

            if (signs.X == 0)
                signs.X = 1;

            if (signs.Y == 0)
                signs.Y = 1;

            if (signs.Z == 0)
                signs.Z = 1;

            return signs;
        }

        public static double Max(this Vector3D normal)
        {
            if (normal.X > normal.Y && normal.X > normal.Z)
                return normal.X;

            if (normal.Y > normal.X && normal.Y > normal.Z)
                return normal.Y;

            return normal.Z;
        }

        public static Vector3D ProjectOntoUnitCube(Vector3D normal)
        {
            double max = Vector3D.Abs(normal).Max();

            return normal / max;
        }

        /// <summary>
        /// Creates a normal from latitude/longitude in radians
        /// </summary>
        public static Vector3D FromLatitudeLongitude(double longitude, double latitude)
        {
            return new Vector3D
            {
                X = Math.Sin(longitude) * Math.Cos(latitude),
                Y = Math.Sin(latitude),
                Z = Math.Cos(longitude) * Math.Cos(latitude),
            };
        }
    }
}
