using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Utils;
using VRageMath;

namespace Jakaria
{
    public class Wake
    {
        public Vector3D position;
        public float minimumRadius;
        public float maximumRadius;
        public Vector3D velocity;
        public Vector4 color;
        public int life = 1;
        public int maxLife;
        public float angle;
        public Vector3 upVector;
        public Vector3 leftVector;

        public Wake(Vector3D Position, Vector3D Velocity, Vector3 GravityDirection, float MinimumRadius = 0f, float MaximumRadius = 1f, int MaxLife = 0)
        {
            position = Position;
            color = Vector4.One;
            velocity = Velocity;
            leftVector = Vector3.Normalize(velocity);
            upVector = Vector3.Normalize(Vector3.Cross(GravityDirection, velocity));

            if (MaxLife == 0)
                maxLife = MyUtils.GetRandomInt(400, 500);
            else
                maxLife = MaxLife;

            angle = MyUtils.GetRandomFloat(0, 360);
            minimumRadius = MinimumRadius;
            maximumRadius = MaximumRadius;
        }
    }
}
