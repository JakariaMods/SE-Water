using Sandbox.Game.Entities;
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
        public const string Version = "1.0.44";
        public const ushort ClientHandlerID = 50270;

        //Materials
        public static readonly MyStringId WaterMaterial = MyStringId.GetOrCompute("JWater3");
        public static readonly MyStringId Water2Material = MyStringId.GetOrCompute("JWater2");
        public static readonly MyStringId BlankMaterial = MyStringId.GetOrCompute("Square");
        public static readonly MyStringId SplashMaterial = MyStringId.GetOrCompute("JSplash");
        public static readonly MyStringId BubblesMaterial = MyStringId.GetOrCompute("JBubbles");
        public static readonly MyStringId WakeMaterial = MyStringId.GetOrCompute("JWake");
        public static readonly MyStringId IconMaterial = MyStringId.GetOrCompute("RedDot");
        public static readonly MyStringId FireflyMaterial = MyStringId.GetOrCompute("Firefly");
        public static readonly MyStringId SeagullMaterial = MyStringId.GetOrCompute("JSeagull");

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
        public static readonly Vector4 WhiteColor = new Vector4(1, 1, 1, 1);

        public enum WaterIntersectionEnum : int
        {
            Overwater = 0,
            ExitsWater = 1,
            EntersWater = 2,
            Underwater = 3,
        }

        public static readonly MyObjectBuilder_Ore IceItem = new MyObjectBuilder_Ore() { SubtypeName = "Ice" };
    }
}
