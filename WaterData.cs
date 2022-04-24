using Jakaria.API;
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
using Jakaria.Configs;
using VRage.Game.ModAPI;

namespace Jakaria
{
    public static class WaterData
    {
        public const string Version = "3.1";
        public const bool EarlyAccess = false;

        public const ushort ClientHandlerID = 50270;

        //Materials
        public static readonly MyStringId WaterMaterial = MyStringId.GetOrCompute("JWater");
        public static readonly MyStringId LavaMaterial = MyStringId.GetOrCompute("JLava");
        public static readonly MyStringId WaterCircleMaterial = MyStringId.GetOrCompute("JWater2");
        public static readonly MyStringId DebugMaterial = MyStringId.GetOrCompute("JDebug");
        public static readonly MyStringId BlankMaterial = MyStringId.GetOrCompute("Square");
        public static readonly MyStringId SplashMaterial = MyStringId.GetOrCompute("JSplash");
        public static readonly MyStringId BubblesMaterial = MyStringId.GetOrCompute("JBubbles");
        public static readonly MyStringId BubbleMaterial = MyStringId.GetOrCompute("JBubble");
        public static readonly MyStringId IconMaterial = MyStringId.GetOrCompute("RedDot");
        public static readonly MyStringId FireflyMaterial = MyStringId.GetOrCompute("Firefly");
        public static readonly MyStringId SeagullMaterial = MyStringId.GetOrCompute("JSeagull");
        public static readonly MyStringId HotTubMaterial = MyStringId.GetOrCompute("JHotTub");
        public static readonly MyStringId GodRayMaterial = MyStringId.GetOrCompute("JGodRay");
        public static readonly MyStringId FoamMaterial = MyStringId.GetOrCompute("JFoam");
        public static readonly MyStringId FishMaterial = MyStringId.GetOrCompute("JFish");
        public static readonly MyStringId PhysicalSplashMaterial = MyStringId.GetOrCompute("JPhysicalSplash");

        //UVs
        public static readonly Vector2 FoamUVSize = new Vector2(0.25f, 0.5f); //4x2 UV Size. Y0 is Heavy Foam & Y1 is Light Foam
        public static readonly Vector2 FishUVSize = new Vector2(0.25f, 0.5f); //4x2 UV Size.

        public static readonly Vector2[] SurfaceUVOffsets = new Vector2[4]
            {
                new Vector2(0.0f, 0.0f),
                new Vector2(0.5f, 0.0f),
                new Vector2(0.0f, 0.5f),
                new Vector2(0.5f, 0.5f),
            };

        public const int MinWaterSplitDepth = 3; //The minimum depth the water can be. It will split if it's less
        public const int MinWaterSplitRadius = 2; //The minimum radius the water can be. It will not split if it's less

        //Sounds
        public static readonly MySoundPair EnvironmentUnderwaterSound = new MySoundPair("JUnderwater");
        public static readonly MySoundPair EnvironmentBeachSound = new MySoundPair("JBeach");
        public static readonly MySoundPair EnvironmentOceanSound = new MySoundPair("JOcean");
        public static readonly MySoundPair EnvironmentUndergroundSound = new MySoundPair("JUnderground");
        public static readonly MySoundPair AmbientSound = new MySoundPair("JAmbient");
        public static readonly MySoundPair GroanSound = new MySoundPair("JGroan");
        public static readonly MySoundPair SplashSound = new MySoundPair("JSplash");
        public static readonly MySoundPair UnderwaterSplashSound = new MySoundPair("JSplashUnderwater");
        public static readonly MySoundPair SeagullSound = new MySoundPair("JSeagull");
        public static readonly MySoundPair UnderwaterExplosionSound = new MySoundPair("JExplosionUnderwater");
        public static readonly MySoundPair UnderwaterPoofSound = new MySoundPair("JPoofUnderwater");
        public static readonly MySoundPair SurfaceExplosionSound = new MySoundPair("JExplosionSurface");

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
        public static readonly Vector4 WaterColor = new Vector4(1, 1, 1, 0.95f);
        public static readonly Vector4 WaterShallowColor = new Vector4(1, 1, 1, 0.94f);
        public static readonly Vector4 WaterDeepColor = new Vector4(1, 1, 1, 0.98f);
        public static readonly Vector4 WaterUnderwaterColor = new Vector4(1, 1, 1, 0.75f);
        public static readonly Vector4 WaterFadeColor = new Vector4(1, 1, 1, 0.8f);
        public static readonly Vector4 WakeColor = new Vector4(0.5f, 0.5f, 0.5f, 0.1f);
        public static readonly Vector4 SmallBubbleColor = new Vector4(0.15f, 0.2f, 0.25f, 0.2f);
        public static readonly Vector4 BlackColor = new Vector4(0, 0, 0, 1);
        public static readonly Vector4 RedColor = new Vector4(1, 0, 0, 1);
        public static readonly Vector4 GreenColor = new Vector4(0, 1, 0, 1);
        public static readonly Vector4 BlueColor = new Vector4(0, 0, 1, 1);

        //public static readonly MyObjectBuilder_Ore IceItem = new MyObjectBuilder_Ore() { SubtypeName = "Ice" };

        public static readonly Vector3D[] Directions = new Vector3D[]
        {
            Vector3D.Up,
            Vector3D.Down,
            Vector3D.Left,
            Vector3D.Right,
            Vector3D.Backward,
            Vector3D.Forward,
        };

        public static readonly Vector3I[] DirectionsI = new Vector3I[]
        {
            Vector3I.Up,
            Vector3I.Down,
            Vector3I.Left,
            Vector3I.Right,
            Vector3I.Backward,
            Vector3I.Forward,
        };

        public const float TSSFontScale = 28.8f / 37f;
        public const char TSSNewLine = '\n';

        public const float PI = 3.1415926f;

        public const double TWOPI = 6.28318530717958647692528676655900576839433879875021164194988918461563281257241799725606965068423413596429617302656461329418768921910116446345071881625696;

        /// <summary>
        /// Hashset for the blocks that should not work in water
        /// </summary>
        public static Dictionary<MyDefinitionId, BlockConfig> BlockConfigs = new Dictionary<MyDefinitionId, BlockConfig>();

        /// <summary>
        /// Dictionary for planet default water settings
        /// </summary>
        public static Dictionary<MyDefinitionId, PlanetConfig> PlanetConfigs = new Dictionary<MyDefinitionId, PlanetConfig>();

        /// <summary>
        /// Dictionary for character water settings
        /// </summary>
        public static Dictionary<MyDefinitionId, CharacterConfig> CharacterConfigs = new Dictionary<MyDefinitionId, CharacterConfig>();

        /// <summary>
        /// Dictionary for respawn pod water settings
        /// </summary>
        public static Dictionary<MyDefinitionId, RespawnPodConfig> RespawnPodConfigs = new Dictionary<MyDefinitionId, RespawnPodConfig>();

        /// <summary>
        /// Dictionary for respawn pod water settings
        /// </summary>
        public static Dictionary<string, MaterialConfig> MaterialConfigs = new Dictionary<string, MaterialConfig>()
        {
            {
                "Water",
                new MaterialConfig()
            }
        };

        /// <summary>
        /// Hashset of available water textures
        /// </summary>
        public static HashSet<string> WaterTextures = new HashSet<string>();

        //500kg Large grid. 20kg Small Grid

        /// <summary>
        /// Multiplier for large grid block's buoyancy
        /// </summary>
        public const float BuoyancyCoefficientLarge = 0.042f;
        /// <summary>
        /// Multiplier for small grid block's buoyancy
        /// </summary>
        public const float BuoyancyCoefficientSmall = 0.2f;

        /// <summary>
        /// Multiplier for large grid airtightness buoyancy
        /// </summary>0
        public const float AirtightnessCoefficientLarge = 0.35f;

        /// <summary>
        /// Multiplier for small grid airtightness buoyancy
        /// </summary>
        public const float AirtightnessCoefficientSmall = 1.0f;

        public const float BuoyancyCoefficientObject = 0.1f;
        public const float CharacterDragCoefficient = 0.075f;

        public const float MinVolumeLarge = 2;
        public const float MinVolumeSmall = 0;

        public const int BuoyancyUpdateFrequencyEntity = 5;

        public const int GridImpactDamageSpeed = 50;

        public const float SphereVolumeOptimizer = (4.0f / 3.0f) * MathHelper.Pi;

        public const float WaterVisibility = 15f;

        /// <summary>
        /// Text that will show in the PSA
        /// </summary
        public const string PSAText = "Hello Engineer!\n\n" +

                                      "The Water Mod will be getting a physics overhaul in {PSADateCountdown} days ({PSADate}). This means that when the update is released, some of your boats may no longer function as expected.\n\n" +

                                      "To help the transition from the current build into the new one, I have made an early-access version available on the workshop under the name 'Water Mod Dev'. Along with early-access, I have released Steam Guides on how to use it.\n\n" +

                                      "I recommend you use the new version of water in the meantime; it will make the transition much easier and your feedback will make it even better.\n\n" +

                                      "Thank you,\n" +
                                      "Jakaria\n\n" +
                                      "The water mod has a discord by the way! (/wdiscord)";

        /// <summary>
        /// Text that will show above the PSA to show the countdown
        /// </summary>
        public const string PSACountdownText = "(This will show {PSATimes} more time(s))";
        /// <summary>
        /// Date the PSA will end
        /// </summary>
        public static readonly DateTime PSADate = new DateTime(2022, 4, 23);
        /// <summary>
        /// How many times a PSA will show
        /// </summary>
        public static readonly int PSAFrequency = 0;

        public const float MaxWakeDistance = 10;
        public const float MaxWakeRadius = 20;
    }
}