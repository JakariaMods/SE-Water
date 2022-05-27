using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Utils;
using VRageMath;
using Sandbox.Game.Entities;
using Sandbox.Game;
using Jakaria;
using VRageRender;
using VRage.Game;
using Sandbox.ModAPI;

namespace Jakaria
{
    public class AnimatedBillboard
    {
        public Vector3D Velocity;

        public bool FadeOut;

        public MyBillboard Billboard;

        public bool InScene;
        public bool MarkedForClose;

        public int Life;
        public int MaxLife;

        protected Vector4 _initialColor;

        public AnimatedBillboard() { }

        public AnimatedBillboard(MyStringId material, Vector2 uvSize, Vector2 uvOffset, MyQuadD quad, Vector3D velocity, int maxLife, bool fadeOut, bool addToScene, Vector4 color, float colorIntensity)
        {
            Velocity = velocity * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS;
            MaxLife = maxLife;
            FadeOut = fadeOut;

            Billboard = new MyBillboard()
            {
                Color = color,
                CustomViewProjection = -1,
                ColorIntensity = colorIntensity,
                Material = material,
                UVSize = uvSize,
                UVOffset = uvOffset,
                Position0 = quad.Point0,
                Position1 = quad.Point1,
                Position2 = quad.Point2,
                Position3 = quad.Point3,
            };

            _initialColor = Billboard.Color;

            if (addToScene)
            {
                MyTransparentGeometry.AddBillboard(Billboard, true);
                InScene = true;
            }
        }

        public AnimatedBillboard(MyStringId material, Vector3D position, Vector3D velocity, Vector3D gravityDirection, int maxLife, float size, Vector2 uvSize, Vector2 uvOffset)
        {
            Vector3 leftVector = Vector3.Normalize(velocity);
            Velocity = velocity * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS;
            MaxLife = maxLife;

            Billboard = new MyBillboard()
            {
                Color = Vector4.One,
                CustomViewProjection = -1,
                ColorIntensity = 1f,
                Material = material,
                UVSize = uvSize,
                UVOffset = uvOffset,
                Position0 = position + leftVector * size,
                Position1 = position,
                Position2 = position + gravityDirection * size,
                Position3 = position + ((leftVector + gravityDirection) * size),
            };

            _initialColor = Billboard.Color;

            InScene = true;
            MyTransparentGeometry.AddBillboard(Billboard, true);
        }

        public virtual void Simulate()
        {
            if (!Vector3.IsZero(Velocity))
            {
                Billboard.Position0 += Velocity;
                Billboard.Position1 += Velocity;
                Billboard.Position2 += Velocity;
                Billboard.Position3 += Velocity;
            }

            Life++;

            if (Life > MaxLife)
            {
                MarkedForClose = true;
            }

            if (InScene)
            {
                if (FadeOut)
                {
                    Billboard.Color = Vector4.Lerp(_initialColor, Vector4.Zero, (float)Life / (float)MaxLife);
                }

                if (MarkedForClose)
                {
                    MyTransparentGeometry.RemovePersistentBillboard(Billboard);
                    InScene = false;
                }
            }
        }
    }
}
