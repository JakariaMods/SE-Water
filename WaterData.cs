using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Utils;
using VRageMath;
using Jakaria.Configs;
using VRageRender;

namespace Jakaria
{
    public static class WaterData
    {
        public const string Version = "3.10";
        public const bool EarlyAccess = false;

        public const ushort ClientHandlerID = 50270;
        public const ushort ServerHandlerID = 50275;

        //Materials
        public static readonly MyStringId BlankMaterial = MyStringId.GetOrCompute("Square");
        public static readonly MyStringId SplashMaterial = MyStringId.GetOrCompute("JSplash");
        public static readonly MyStringId BubblesMaterial = MyStringId.GetOrCompute("JBubbles");
        public static readonly MyStringId BubbleMaterial = MyStringId.GetOrCompute("JBubble");
        public static readonly MyStringId IconMaterial = MyStringId.GetOrCompute("RedDot");
        public static readonly MyStringId FireflyMaterial = MyStringId.GetOrCompute("Firefly");
        public static readonly MyStringId SeagullMaterial = MyStringId.GetOrCompute("JSeagull");
        public static readonly MyStringId GodRayMaterial = MyStringId.GetOrCompute("JGodRay");
        public static readonly MyStringId FoamMaterial = MyStringId.GetOrCompute("JFoam");
        public static readonly MyStringId WakeMaterial = MyStringId.GetOrCompute("JWake");
        public static readonly MyStringId PhysicalSplashMaterial = MyStringId.GetOrCompute("JPhysicalSplash");

        //UVs
        public static readonly Vector2 FoamUVSize = new Vector2(0.25f, 0.5f); //4x2 UV Size. Y0 is Heavy Foam & Y1 is Light Foam

        public static readonly Vector2[] SurfaceUVOffsets = new Vector2[4]
            {
                new Vector2(0.0f, 0.0f),
                new Vector2(0.5f, 0.0f),
                new Vector2(0.0f, 0.5f),
                new Vector2(0.5f, 0.5f),
            };

        public const int MinWaterSplitDepth = 3; //The minimum depth the water can be. It will split if it's less
        public const int MinWaterSplitRadius = 3; //The minimum radius the water can be. It will not split if it's less

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

        public static readonly Vector4 BubbleColor = new Vector4(0.05f, 0.0625f, 0.075f, 0.5f);
        public static readonly Vector4 WhiteColor = Vector4.One;
        public static readonly Vector4 WaterColor = new Vector4(1, 1, 1, 0.95f);
        public static readonly Vector4 WaterShallowColor = new Vector4(1, 1, 1, 0.94f);
        public static readonly Vector4 WaterDeepColor = new Vector4(1, 1, 1, 0.98f);
        public static readonly Vector4 WaterUnderwaterColor = new Vector4(1, 1, 1, 0.75f);
        public static readonly Vector4 WaterFadeColor = new Vector4(1, 1, 1, 0.8f);
        public static readonly Vector4 WakeColor = new Vector4(0.3f, 0.3f, 0.3f, 0.3f);
        public static readonly Vector4 SmallBubbleColor = new Vector4(0.15f, 0.2f, 0.25f, 0.2f);
        public static readonly Vector4 BlackColor = new Vector4(0, 0, 0, 1);
        public static readonly Vector4 RedColor = new Vector4(1, 0, 0, 1);
        public static readonly Vector4 GreenColor = new Vector4(0, 1, 0, 1);
        public static readonly Vector4 BlueColor = new Vector4(0, 0, 1, 1);

        /// <summary>
        /// Collection of extended block definition data
        /// </summary>
        public static Dictionary<MyDefinitionId, BlockConfig> BlockConfigs = new Dictionary<MyDefinitionId, BlockConfig>();

        /// <summary>
        /// Dictionary for planet default water settings including how it should treat LOD.
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
                new MaterialConfig(true)
            },
        };

        public static Dictionary<string, FishConfig> FishConfigs = new Dictionary<string, FishConfig>();

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

        public const float MinVolumeLarge = 2;
        public const float MinVolumeSmall = 0;

        public const int BuoyancyUpdateFrequencyEntity = 5;

        public const int GridImpactDamageSpeed = 50;

        public const float SphereVolumeOptimizer = (4.0f / 3.0f) * MathHelper.Pi;

        public const float WaterVisibility = 5f;

        public const float DragMultiplier = 0.2f;

        public const MyBillboard.BlendTypeEnum BlendType = MyBillboard.BlendTypeEnum.Standard;

        public static readonly Vector3I[] Base3Directions = new Vector3I[3]
        {
            new Vector3I(1, 0, 0),
            new Vector3I(0, 1, 0),
            new Vector3I(0, 0, 1)
        };

        public const string SaveVariableName = "JWater2";

        public const string StartMessage = "";
    }
}