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
    /// Extension methods for <see cref="Vector3"/>
    /// </summary>
    public static class Vector3Extensions
    {
        /// <summary>
        /// Returns the sign of the vector with the greatest distance from zero. 
        /// EX: {-6, 5, 8} => {0, 0, 1 }
        /// EX: {-6, 5, 4} => {-1, 0, 0}
        /// </summary>
        public static Vector3 MaxComponentSign(this Vector3 vector)
        {
            return vector * vector.MaxComponent();
        }
        
        /// <summary>
        /// Returns the vector with the greatest distance from zero
        /// </summary>
        public static Vector3 MaxComponent(this Vector3 vector)
        {
            Vector3 abs = Vector3.Abs(vector);

            if (abs.X > abs.Y && abs.X > abs.Z)
                return Vector3.UnitX;

            if (abs.Y > abs.X && abs.Y > abs.Z)
                return Vector3.UnitY;

            if (abs.Z > abs.X && abs.Z > abs.Y)
                return Vector3.UnitZ;

            return Vector3.Zero;
        }

        /// <summary>
        /// Gets the sign of each vector
        /// </summary>
        public static Vector3 Sign(this Vector3 vector)
        {
            return new Vector3
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
        public static Vector3 SignNonZero(this Vector3 vector)
        {
            Vector3 signs = new Vector3
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

        public static float Max(this Vector3 normal)
        {
            if (normal.X > normal.Y && normal.X > normal.Z)
                return normal.X;

            if (normal.Y > normal.X && normal.Y > normal.Z)
                return normal.Y;

            return normal.Z;
        }

        public static Vector3 ProjectOntoUnitCube(Vector3 normal)
        {
            float max = Vector3.Abs(normal).Max();

            return normal / max;
        }

        /// <summary>
        /// Creates a normal from latitude/longitude in radians
        /// </summary>
        public static Vector3 FromLatitudeLongitude(float longitude, float latitude)
        {
            return new Vector3
            {
                X = (float)Math.Sin(longitude) * (float)Math.Cos(latitude),
                Y = (float)Math.Sin(latitude),
                Z = (float)Math.Cos(longitude) * (float)Math.Cos(latitude),
            };
        }
    }
}
