using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace Jakaria
{
    public static class WaterData
    {
        public const string Version = "2.10";
        public const int MinAPIVersion = 1;
        public const ushort ClientHandlerID = 50270;

        //Materials
        public static readonly MyStringId WaterMaterial = MyStringId.GetOrCompute("JWater");
        public static readonly MyStringId LavaMaterial = MyStringId.GetOrCompute("JLava");
        public static readonly MyStringId WaterCircleMaterial = MyStringId.GetOrCompute("JWater2");
        public static readonly MyStringId DebugMaterial = MyStringId.GetOrCompute("JDebug");
        public static readonly MyStringId BlankMaterial = MyStringId.GetOrCompute("Square");
        public static readonly MyStringId SplashMaterial = MyStringId.GetOrCompute("JSplash");
        public static readonly MyStringId BubblesMaterial = MyStringId.GetOrCompute("JBubbles");
        public static readonly MyStringId WakeMaterial = MyStringId.GetOrCompute("JWake");
        public static readonly MyStringId IconMaterial = MyStringId.GetOrCompute("RedDot");
        public static readonly MyStringId FireflyMaterial = MyStringId.GetOrCompute("Firefly");
        public static readonly MyStringId SeagullMaterial = MyStringId.GetOrCompute("JSeagull");
        public static readonly MyStringId ShadowMaterial = MyStringId.GetOrCompute("JShadow");

        public static float DotMaxFOV = UpdateFovFrustum();

        public static float UpdateFovFrustum()
        {
            DotMaxFOV = (float)(Math.Sin(((MyAPIGateway.Session.Camera.FieldOfViewAngle + 45) * 2) * (Math.PI / 180f)));
            return DotMaxFOV;
        }

        public static readonly MyStringId[] FishMaterials = new MyStringId[]
        {
            MyStringId.GetOrCompute("JFish_0"),
            MyStringId.GetOrCompute("JFish_1"),
            MyStringId.GetOrCompute("JFish_2"),
            MyStringId.GetOrCompute("JFish_3"),
            MyStringId.GetOrCompute("JFish_4"),
            MyStringId.GetOrCompute("JFish_5"),
            MyStringId.GetOrCompute("JFish_6"),
            MyStringId.GetOrCompute("JFish_7"),
            MyStringId.GetOrCompute("JFish_8"),
            MyStringId.GetOrCompute("JFish_9"),
        };

        //Sounds
        public static readonly MySoundPair EnvironmentUnderwaterSound = new MySoundPair("JUnderwater");
        public static readonly MySoundPair EnvironmentBeachSound = new MySoundPair("JBeach");
        public static readonly MySoundPair EnvironmentOceanSound = new MySoundPair("JOcean");
        public static readonly MySoundPair AmbientSound = new MySoundPair("JAmbient");
        public static readonly MySoundPair GroanSound = new MySoundPair("JGroan");
        public static readonly MySoundPair SplashSound = new MySoundPair("JSplash");
        public static readonly MySoundPair SeagullSound = new MySoundPair("JSeagull");
        public static readonly MySoundPair SizzleSound = new MySoundPair("JSizzle");

        public static readonly string[] DropContainerNames = new string[]
        {
            "Container_MK-1",
            "Container_MK-2",
            "Container_MK-3",
            "Container_MK-4",
            //"Container_MK-5", Space Only
            //"Container_MK-6", Space Only
            //"Container_MK-7", Strong-Signal Only
            "Container_MK-8",
            "Container_MK-9",
            //"Container_MK-10", Space Only
            "Container_MK-11",
            "Container_MK-12",
            "Container_MK-13",
            //"Container_MK-14", Space Only
            //"Container_MK-15", Space Only
            "Container_MK-16",
            "Container_MK-17",
            "Container_MK-18",
            //"Container_MK-19", Space Only
        };

        public static readonly Vector4 BubbleColor = new Vector4(0.05f, 0.0625f, 0.075f, 0.5f);
        public static readonly Vector4 WhiteColor = Vector4.One;
        public static readonly Vector4 WaterColor = new Vector4(1, 1, 1, 0.9f);
        public static readonly Vector4 WaterUnderwaterColor = new Vector4(1, 1, 1, 0.75f);
        public static readonly Vector4 WaterFadeColor = new Vector4(1, 1, 1, 0.8f);

        public static readonly MyObjectBuilder_Ore IceItem = new MyObjectBuilder_Ore() { SubtypeName = "Ice" };

        public static readonly Vector3D[] Directions = new Vector3D[]
        {
            Vector3D.Up,
            Vector3D.Down,
            Vector3D.Left,
            Vector3D.Right,
            Vector3D.Backward,
            Vector3D.Forward,
        };

        public const float TSSFontScale = 28.8f / 37f;
        public const char TSSNewLine = '\n';

        public const double TwoPi = 6.28318530717958647692528676655900576839433879875021164194988918461563281257241799725606965068423413596429617302656461329418768921910116446345071881625696;

        /*public static readonly Dictionary<int, float> WaterLODDistance = new Dictionary<int, float>()
        {
            { 0, float.PositiveInfinity },
            { 1, 500000f * 500000f },
            { 2, 25000f * 25000f },
            { 3, 2500f * 2500f },
            { 4, 1250f * 1250f},
            { 5, 800f * 800f},
            { 6, 500f * 500f},
            { 7, 400f * 400f},
            { 8, 250f * 250f},
            { 9, 100f *  100},
            { 10, 50f  * 50},
        };

        public static readonly Dictionary<int, float> WaterLODQuality = new Dictionary<int, float>()
        {
            { 10000, float.PositiveInfinity },
            { 1000, 10000f },
            { 100, 500f },
            { 50, 400},
            { 25, 200},
            { 10, 100},
            { 2, 50}
        };*/

    }
}
