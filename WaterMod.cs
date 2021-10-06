using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Utils;
using VRageMath;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using Sandbox.Game.EntityComponents;
using System.IO;
using Draygo.Drag.API;
using System.Collections.Concurrent;
using System.Linq;
using Draygo.API;
using System.Text;
using VRage.Collections;
using VRage;
using Sandbox.Game.Entities.Character.Components;
using VRage.ObjectBuilders;
using Jakaria.Utils;
using Jakaria.API;
using VRage.Serialization;
using Sandbox.Definitions;
using CollisionLayers = Sandbox.Engine.Physics.MyPhysics.CollisionLayers;
using WeaponCore.Api;
using static Jakaria.FatBlockStorage;
using ProtoBuf;

namespace Jakaria
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    class WaterMod : MySessionComponentBase
    {
        List<IMyPlayer> players = new List<IMyPlayer>();

        //Threading Stuff
        ConcurrentCachingList<SimulateGridThread> simulateGridThreads = new ConcurrentCachingList<SimulateGridThread>();
        ConcurrentCachingList<SimulatePlayerThread> simulatePlayerThreads = new ConcurrentCachingList<SimulatePlayerThread>();

        //Water Effects
        //public List<Water> waters = new List<Water>();
        public Dictionary<long, Water> Waters = new Dictionary<long, Water>();
        public Dictionary<long, MyTuple<float, float>> WheelStorage = new Dictionary<long, MyTuple<float, float>>();

        //public Dictionary<Vector3I, float> Wakes = new Dictionary<Vector3I, float>();
        List<Splash> Splashes = new List<Splash>(128);
        Seagull[] Seagulls = new Seagull[1];
        Fish[] Fishes = new Fish[1];
        //GodRay[] GodRays = new GodRay[1];
        List<Bubble> Bubbles = new List<Bubble>(512);
        SmallBubble[] SmallBubbles = new SmallBubble[1];
        //List<Wake> Wakes = new List<Wake>(512);
        List<COBIndicator> indicators = new List<COBIndicator>();
        public ConcurrentStack<QuadBillboard> QuadBillboards = new ConcurrentStack<QuadBillboard>();
        public List<SimulatedSplash> SimulatedSplashes = new List<SimulatedSplash>(1024);
        public ConcurrentStack<MyTuple<MyCubeBlock, float>> DamageQueue = new ConcurrentStack<MyTuple<MyCubeBlock, float>>();

        FastNoiseLite GeneralNoise = new FastNoiseLite();
        double GeneralNoiseTimer = 0;
        Vector3D zeroVector = Vector3D.Zero;

        public struct QuadBillboard
        {
            public MyStringId Material;
            public MyQuadD Quad;
            public Vector4 Color;

            public QuadBillboard(MyStringId Material, ref MyQuadD Quad, ref Vector4 Color)
            {
                this.Material = Material;
                this.Quad = Quad;
                this.Color = Color;
            }

            public QuadBillboard(ref MyStringId Material, ref MyQuadD Quad, Vector4 Color)
            {
                this.Material = Material;
                this.Quad = Quad;
                this.Color = Color;
            }

            public QuadBillboard(ref MyStringId Material, ref MyQuadD Quad, ref Vector4 Color)
            {
                this.Material = Material;
                this.Quad = Quad;
                this.Color = Color;
            }

            public QuadBillboard(MyStringId Material, ref MyQuadD Quad, Vector4 Color)
            {
                this.Material = Material;
                this.Quad = Quad;
                this.Color = Color;
            }
        }

        //Draygo
        ConcurrentDictionary<IMyEntity, DragClientAPI.DragObject> dragAPIGetter = new ConcurrentDictionary<IMyEntity, DragClientAPI.DragObject>();
        DragClientAPI DragAPI = new DragClientAPI();
        WcApi WeaponCoreAPI = new WcApi();

        Object effectLock = new Object();

        [ProtoContract]
        public static class Settings
        {
            [ProtoMember(1)]
            public static float Quality = 0.5f;

            [ProtoMember(2)]
            public static bool ShowHud = true;

            [ProtoMember(3)]
            public static bool ShowCenterOfBuoyancy = false;

            [ProtoMember(4)]
            public static bool ShowDepth = true;

            [ProtoMember(5)]
            public static bool ShowFog = true;

            [ProtoMember(6)]
            public static bool ShowDebug = false;

            [ProtoMember(7)]
            public static float Volume = 1f;

            [ProtoMember(8)]
            public static bool ShowAltitude = true;
        }

        public static class Session
        {
            public static bool CameraAboveWater = false;
            public static Vector3D CameraClosestPosition = Vector3D.Zero;
            public static bool CameraUnderground = false;
            public static float CameraDepth = 0;
            public static bool CameraAirtight = false;
            public static bool CameraUnderwater = false;
            public static bool CameraHottub = false;
            public static int InsideGrid = 0;
            public static int InsideVoxel = 0;
            public static Vector3D CameraPosition = Vector3.Zero;
            public static MatrixD CameraMatrix;
            public static Vector3 CameraRotation = Vector3.Zero;
            public static Vector3 SunDirection = Vector3.Zero;
            public static float SessionTimer = 0;
            public static Vector3D GravityDirection = Vector3.Zero;
            public static Vector3 Gravity = Vector3.Zero;
            public static Vector3D GravityAxisA = Vector3D.Zero;
            public static Vector3D GravityAxisB = Vector3D.Zero;
            public static Vector3D LastLODBuildPosition = Vector3D.MaxValue;
            public static float DistanceToHorizon = 0;
        }

        //Client variables
        //Vector3[] meshVertices = new Vector3[10 * 10];

        MyPlanet closestPlanet = null;
        Water closestWater = null;
        MyObjectBuilder_WeatherEffect closestWeather = null;
        bool previousUnderwaterState = true;
        public List<MyEntity> nearbyEntities = new List<MyEntity>();
        bool initialized = false;
        float nightValue = 0;
        IMyWeatherEffects weatherEffects;
        MyEntity sunOccluder;
        MyEntity particleOccluder;

        //Text Hud API
        HudAPIv2 TextAPI;
        HudAPIv2.HUDMessage DepthMeter;

        //Timers
        int tickTimer = 0;
        public const int tickTimerMax = 5;
        int tickTimerSecond = 0;
        int tickTimerPlayerDamage = 0;
        public float ambientTimer = 0;
        public float ambientBoatTimer = 0;

        public MyEntity3DSoundEmitter EnvironmentUnderwaterSoundEmitter = new MyEntity3DSoundEmitter(null);
        public MyEntity3DSoundEmitter EnvironmentUndergroundSoundEmitter = new MyEntity3DSoundEmitter(null);
        public MyEntity3DSoundEmitter EnvironmentOceanSoundEmitter = new MyEntity3DSoundEmitter(null);
        public MyEntity3DSoundEmitter EnvironmentBeachSoundEmitter = new MyEntity3DSoundEmitter(null);
        public MyEntity3DSoundEmitter AmbientSoundEmitter = new MyEntity3DSoundEmitter(null);

        public MyEntity3DSoundEmitter AmbientBoatSoundEmitter = new MyEntity3DSoundEmitter(null);
        public MyEntity3DSoundEmitter MusicEmitter = new MyEntity3DSoundEmitter(null);

        public string musicCategory = "";

        public static WaterMod Static;

        public WaterMod()
        {
            if (Static == null)
                Static = this;
        }

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.WaterModVersion.Replace("{0}", WaterData.Version + (WaterData.EarlyAccess ? "EA" : "")));

            bool valid = false;
            if (MyAPIGateway.Utilities.IsDedicated || !MyAPIGateway.Session.IsServer)
                foreach (var mod in MyAPIGateway.Session.Mods)
                {
                    if (mod.PublishedFileId == 2200451495 || mod.PublishedFileId == 2379346707)
                        valid = true;
                }
            else
                valid = true;

            if (!valid)
                throw new InvalidModException();

            MyAPIGateway.Multiplayer.RegisterMessageHandler(WaterData.ClientHandlerID, ClientHandler);

            MyLog.Default.WriteLine(WaterLocalization.CurrentLanguage.WaterModVersion.Replace("{0}", WaterData.Version + (WaterData.EarlyAccess ? "EA" : "")));

            weatherEffects = MyAPIGateway.Session.WeatherEffects;

            //Client
            if (!MyAPIGateway.Utilities.IsDedicated)
            {
                TextAPI = new HudAPIv2(TextAPIRegisteredCallback);
                sunOccluder = CreateOccluderEntity();
                particleOccluder = CreateOccluderEntity();

                MyAPIGateway.Utilities.MessageEntered += Utilities_MessageEntered;
                //MyExplosions.OnExplosion += MyExplosions_OnExplosion;
                WaterData.UpdateFovFrustum();

                RecreateWater();
            }

            //Server
            if (MyAPIGateway.Session.IsServer)
            {
                MyVisualScriptLogicProvider.RespawnShipSpawned += RespawnShipSpawned;
                MyVisualScriptLogicProvider.PrefabSpawnedDetailed += PrefabSpawnedDetailed;
                MyEntities.OnEntityAdd += Entities_OnEntityAdd;
                MyVisualScriptLogicProvider.PlayerConnected += PlayerConnected;
                MyVisualScriptLogicProvider.PlayerDisconnected += PlayerDisconnected;
            }

            initialized = true;
        }

        private void MyExplosions_OnExplosion(ref MyExplosionInfo explosionInfo)
        {
            float ExplosionRadius = (float)explosionInfo.ExplosionSphere.Radius;
            Vector3D ExplosionPosition = explosionInfo.ExplosionSphere.Center;

            if (closestWater != null)
            {
                float Depth = closestWater.GetDepth(ref ExplosionPosition);

                if (Depth - ExplosionRadius <= 0)
                {
                    explosionInfo.CreateParticleEffect = false;

                    if (Depth <= 0)
                        explosionInfo.CustomSound = WaterData.UnderwaterExplosionSound;

                    CreatePhysicsSplash(closestWater.GetClosestSurfacePoint(explosionInfo.ExplosionSphere.Center), -Session.GravityDirection, (float)explosionInfo.ExplosionSphere.Radius / 4, (int)explosionInfo.ExplosionSphere.Radius * 4);
                    CreateSplash(closestWater.GetClosestSurfacePoint(explosionInfo.ExplosionSphere.Center), (float)explosionInfo.ExplosionSphere.Radius, true);
                    //CreateBubble(closestWater.GetClosestSurfacePoint(explosionInfo.ExplosionSphere.Center), (float)explosionInfo.ExplosionSphere.Radius);
                }
            }

            return;
        }

        private void TextAPIRegisteredCallback()
        {
            if (TextAPI.Heartbeat)
            {
                DepthMeter = new HudAPIv2.HUDMessage(new StringBuilder(""), Vector2D.Zero);
            }
        }

        private void PrefabSpawnedDetailed(long entityId, string prefabName)
        {
            /*if (prefabName == null)
                return;

            if (WaterData.DropContainerNames.Contains(prefabName))
            {
                MyEntity entity = MyEntities.GetEntityById(entityId);
                MyPlanet planet = MyGamePruningStructure.GetClosestPlanet(entity.PositionComp.GetPosition());

                if (planet == null || entity == null)
                    return;

                if (Waters != null)
                    foreach (var water in Waters.Values)
                    {
                        if (water.IsUnderwater(planet.GetClosestSurfacePointGlobal(entity.PositionComp.GetPosition())))
                        {
                            MyVisualScriptLogicProvider.SpawnPrefabInGravity("Floating Container_Mk-12", water.GetClosestSurfacePoint(entity.PositionComp.GetPosition(), 250), water.GetUpDirection(entity.PositionComp.GetPosition()));
                            MyEntities.RaiseEntityRemove(entity);
                            return;
                        }
                    }
            }*/
        }

        /// <summary>
        /// Returns closest water to a position
        /// </summary>
        public Water GetClosestWater(Vector3 position)
        {
            float depth = float.PositiveInfinity;
            Water closestWater = null;
            foreach (var water in Waters.Values)
            {
                float depth2 = water.GetDepthSquared(position);

                if (depth2 < depth)
                {
                    closestWater = water;

                    if (depth2 < 0)
                        break;

                    depth = depth2;
                }
            }

            return closestWater;
        }

        /// <summary>
        /// Returns closest player to a position
        /// </summary>
        public IMyPlayer GetClosestPlayer(Vector3 position)
        {
            if (!MyAPIGateway.Multiplayer.MultiplayerActive)
                return MyAPIGateway.Session.Player;

            float dist = float.MaxValue;
            IMyPlayer player = null;
            foreach (var playerLoop in players)
            {
                if (Vector3.DistanceSquared(position, player.GetPosition()) < dist)
                    player = playerLoop;
            }
            return player;
        }

        public override void BeforeStart()
        {
            //MyAPIGateway.Utilities.SendModMessage(WaterModAPI.ModHandlerID, WaterModAPI.ModAPIVersion);
            WaterModAPIBackend.BeforeStart();
        }

        private void Entities_OnEntityAdd(MyEntity obj)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                if (obj is MyPlanet)
                {
                    MyPlanet planet = obj as MyPlanet;

                    if (WaterUtils.HasWater(planet))
                        return;

                    foreach (var weather in planet.Generator.WeatherGenerators)
                    {
                        if (weather.Voxel == "WATERMODDATA")
                        {
                            WaterSettings settings = MyAPIGateway.Utilities.SerializeFromXML<WaterSettings>(weather.Weathers?[0]?.Name);

                            Waters[planet.EntityId] = new Water(planet, settings);
                            SyncClients();
                        }
                    }

                    return;
                }
            }
        }

        /// <summary>
        /// Commands
        /// </summary>
        public void Utilities_MessageEntered(string messageText, ref bool sendToOthers)
        {
            if (!messageText.StartsWith("/") || messageText.Length == 0)
                return;

            string[] args = messageText.TrimStart('/').Split(' ');

            if (args.Length == 0)
                return;

            switch (args[0])
            {
                case "whelp":
                    sendToOthers = false;

                    MyVisualScriptLogicProvider.OpenSteamOverlayLocal(@"https://steamcommunity.com/sharedfiles/filedetails/?id=2574095672");
                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.OpenGuide);

                    break;
                case "wversion":
                    sendToOthers = false;

                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.WaterModVersion.Replace("{0}", WaterData.Version));

                    break;
                case "wlanguage":
                    sendToOthers = false;

                    //Set Language
                    if (args.Length == 2)
                    {
                        Language language;

                        if (WaterLocalization.Languages.TryGetValue(WaterUtils.ValidateCommandData(args[1]), out language))
                        {
                            WaterLocalization.CurrentLanguage = language;

                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetLanguage.Replace("{0}", args[1]).Replace("{1}", WaterLocalization.CurrentLanguage.TranslationAuthor));
                            SaveSettings();
                        }
                        else
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetLanguageNoParse.Replace("{0}", args[1]));
                    }
                    break;

                case "wquality":
                    sendToOthers = false;

                    //Get Quality
                    if (args.Length == 1)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GetQuality.Replace("{0}", Settings.Quality.ToString()));
                    }

                    //Set Quality
                    if (args.Length == 2)
                    {
                        float quality;
                        if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out quality))
                        {
                            quality = MathHelper.Clamp(quality, 0.4f, 1.5f);
                            Settings.Quality = quality;

                            RecreateWater();
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetQuality.Replace("{0}", Settings.Quality.ToString()));
                            SaveSettings();
                        }
                        else
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetQualityNoParse.Replace("{0}", args[1]));
                    }
                    break;

                case "wvolume":
                    sendToOthers = false;

                    //Get Volume
                    if (args.Length == 1)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GetVolume.Replace("{0}", Settings.Volume.ToString()));
                    }

                    //Set Volume
                    if (args.Length == 2)
                    {
                        float volume;
                        if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out volume))
                        {
                            volume = MathHelper.Clamp(volume, 0f, 1f);
                            Settings.Volume = volume;

                            RecreateWater();
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetVolume.Replace("{0}", Settings.Volume.ToString()));
                            SaveSettings();
                        }
                        else
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetVolumeNoParse.Replace("{0}", args[1]));
                    }
                    break;

                case "wcob":
                    sendToOthers = false;

                    //Toggle Center of Buoyancy Debug
                    if (args.Length == 1)
                    {
                        Settings.ShowCenterOfBuoyancy = !Settings.ShowCenterOfBuoyancy;
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.ToggleRenderCOB);
                    }
                    break;

                case "wdebug":
                    sendToOthers = false;

                    //Toggle Debug Mode
                    if (args.Length == 1)
                    {
                        Settings.ShowDebug = !Settings.ShowDebug;
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.ToggleDebug);
                    }
                    break;

                case "wdepth":
                    sendToOthers = false;

                    //Toggle Show Depth
                    if (TextAPI.Heartbeat)
                    {
                        Settings.ShowDepth = !Settings.ShowDepth;
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.ToggleShowDepth);
                        SaveSettings();
                    }
                    else
                    {
                        Settings.ShowDepth = false;
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoTextAPI);
                    }

                    break;

                case "waltitude":
                    sendToOthers = false;

                    //Toggle Show Depth
                    if (TextAPI.Heartbeat)
                    {
                        Settings.ShowAltitude = !Settings.ShowAltitude;
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.ToggleShowAltitude);
                        SaveSettings();
                    }
                    else
                    {
                        Settings.ShowAltitude = false;
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoTextAPI);
                    }

                    break;
            }

            //SpaceMaster level and up
            switch (args[0])
            {
                //Toggle fog
                case "wfog":
                    sendToOthers = false;

                    if (MyAPIGateway.Session.PromoteLevel < MyPromoteLevel.Moderator)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GenericNoPermissions);
                        return;
                    }

                    if (MyAPIGateway.Session.CreativeMode)
                    {
                        Settings.ShowFog = !Settings.ShowFog;
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.ToggleFog);
                    }
                    else
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GenericNoCreative);
                    }

                    break;

                //Toggle birds on a planet
                case "wbird":
                    sendToOthers = false;

                    if (MyAPIGateway.Session.PromoteLevel < MyPromoteLevel.Moderator)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GenericNoPermissions);
                        return;
                    }

                    if (closestPlanet != null)
                    {
                        foreach (var water in Waters.Values)
                        {
                            if (water.planetID == closestPlanet.EntityId)
                            {
                                water.enableSeagulls = !water.enableSeagulls;
                                MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(Waters))); RebuildLOD();
                                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.ToggleBirds);
                                return;
                            }
                        }

                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                    }
                    else
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);

                    break;

                //Toggle fish on a planet
                case "wfish":
                    sendToOthers = false;

                    if (MyAPIGateway.Session.PromoteLevel < MyPromoteLevel.Moderator)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GenericNoPermissions);
                        return;
                    }

                    if (closestPlanet != null)
                    {
                        foreach (var water in Waters.Values)
                        {
                            if (water.planetID == closestPlanet.EntityId)
                            {
                                water.enableFish = !water.enableFish;
                                MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(Waters))); RebuildLOD();
                                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.ToggleFish);
                                return;
                            }
                        }

                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                    }
                    else
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);

                    break;

                //Toggle surface foam on a planet
                case "wfoam":
                    sendToOthers = false;

                    if (MyAPIGateway.Session.PromoteLevel < MyPromoteLevel.Moderator)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GenericNoPermissions);
                        return;
                    }

                    if (closestPlanet != null)
                    {
                        foreach (var water in Waters.Values)
                        {
                            if (water.planetID == closestPlanet.EntityId)
                            {
                                water.enableFoam = !water.enableFoam;
                                MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(Waters))); RebuildLOD();
                                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.ToggleFoam);
                                return;
                            }
                        }

                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                    }
                    else
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);

                    break;

                case "wbuoyancy":
                    sendToOthers = false;

                    if (MyAPIGateway.Session.PromoteLevel < MyPromoteLevel.Moderator)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GenericNoPermissions);
                        return;
                    }

                    //Get Buoyancy
                    if (args.Length == 1)
                    {
                        if (closestPlanet == null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                            return;
                        }

                        if (closestWater != null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GetBuoyancy.Replace("{0}", closestWater.buoyancy.ToString()));
                            return;
                        }

                        //If foreach loop doesn't find the planet, assume it doesn't exist
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                    }

                    //Set Buoyancy
                    if (args.Length == 2)
                    {
                        float buoyancy;
                        if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out buoyancy))
                        {
                            if (closestPlanet == null)
                            {
                                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                                return;
                            }

                            foreach (var water in Waters.Values)
                            {
                                if (water.planetID == closestPlanet.EntityId)
                                {
                                    water.buoyancy = MathHelper.Clamp(buoyancy, 0f, 10f);
                                    MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(Waters))); RebuildLOD();
                                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetBuoyancy.Replace("{0}", water.buoyancy.ToString()));
                                    return;
                                }
                            }

                            //If foreach loop doesn't find the planet, assume it doesn't exist
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                        }
                        else
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetBuoyancyNoParse.Replace("{0}", args[1]));
                    }
                    break;

                case "wviscosity":
                    sendToOthers = false;

                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.RetiredCommand);
                    break;

                case "wdensity":
                    sendToOthers = false;

                    if (MyAPIGateway.Session.PromoteLevel < MyPromoteLevel.Moderator)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GenericNoPermissions);
                        return;
                    }

                    //Get Viscosity
                    if (args.Length == 1)
                    {
                        if (closestPlanet == null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                            return;
                        }

                        if (closestWater != null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GetDensity.Replace("{0}", closestWater.fluidDensity.ToString()));
                            return;
                        }

                        //If foreach loop doesn't find the planet, assume it doesn't exist
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                    }

                    //Set Viscosity
                    if (args.Length == 2)
                    {
                        float viscosity;
                        if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out viscosity))
                        {
                            if (closestPlanet == null)
                            {
                                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                                return;
                            }

                            foreach (var water in Waters.Values)
                            {
                                if (water.planetID == closestPlanet.EntityId)
                                {
                                    water.fluidDensity = viscosity;
                                    MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(Waters))); RebuildLOD();
                                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetDensity.Replace("{0}", water.fluidDensity.ToString()));
                                    return;
                                }
                            }

                            //If foreach loop doesn't find the planet, assume it doesn't exist
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                        }
                        else
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetDensityNoParse.Replace("{0}", args[1]));
                    }
                    break;

                case "wradius":
                    sendToOthers = false;

                    if (MyAPIGateway.Session.PromoteLevel < MyPromoteLevel.Moderator)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GenericNoPermissions);
                        return;
                    }

                    //Get Radius
                    if (args.Length == 1)
                    {
                        if (closestPlanet == null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                            return;
                        }

                        if (closestWater != null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GetRadius.Replace("{0}", (closestWater.radius / closestPlanet.MinimumRadius).ToString()));
                            return;
                        }

                        //If foreach loop doesn't find the planet, assume it doesn't exist
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                    }

                    //Set Radius
                    if (args.Length == 2)
                    {
                        float radius;
                        if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out radius))
                        {
                            radius = MathHelper.Clamp(radius, 0.95f, 1.75f);

                            if (closestPlanet == null)
                            {
                                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                                return;
                            }

                            foreach (var water in Waters.Values)
                            {
                                if (water.planetID == closestPlanet.EntityId)
                                {

                                    water.radius = radius * closestPlanet.MinimumRadius;
                                    MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(Waters))); RebuildLOD();
                                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetRadius.Replace("{0}", (water.radius / closestPlanet.MinimumRadius).ToString()));
                                    return;
                                }
                            }

                            //If foreach loop doesn't find the planet, assume it doesn't exist
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                        }
                        else
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetRadiusNoParse.Replace("{0}", args[1]));
                    }
                    break;

                case "wwaveheight":
                    sendToOthers = false;

                    if (MyAPIGateway.Session.PromoteLevel < MyPromoteLevel.Moderator)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GenericNoPermissions);
                        return;
                    }

                    //Get Oscillation
                    if (args.Length == 1)
                    {
                        if (closestPlanet == null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                            return;
                        }

                        if (closestWater != null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GetWaveHeight.Replace("{0}", closestWater.waveHeight.ToString()));
                            return;

                        }

                        //If foreach loop doesn't find the planet, assume it doesn't exist
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                    }

                    //Set Oscillation
                    if (args.Length == 2)
                    {
                        float waveHeight;
                        if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out waveHeight))
                        {
                            if (closestPlanet == null)
                            {
                                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                                return;
                            }

                            foreach (var water in Waters.Values)
                            {
                                if (water.planetID == closestPlanet.EntityId)
                                {
                                    water.waveHeight = waveHeight;
                                    MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(Waters))); RebuildLOD();
                                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetWaveHeight.Replace("{0}", water.waveHeight.ToString()));

                                    return;
                                }
                            }
                            //If foreach loop doesn't find the planet, assume it doesn't exist
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                        }
                        else
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetWaveHeightNoParse.Replace("{0}", args[1]));
                    }
                    break;
                case "wtideheight":
                    sendToOthers = false;

                    if (MyAPIGateway.Session.PromoteLevel < MyPromoteLevel.Moderator)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GenericNoPermissions);
                        return;
                    }

                    //Get Tide Height
                    if (args.Length == 1)
                    {
                        if (closestPlanet == null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                            return;
                        }

                        if (closestWater != null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GetTideHeight.Replace("{0}", closestWater.tideHeight.ToString()));
                            return;

                        }

                        //If foreach loop doesn't find the planet, assume it doesn't exist
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                    }

                    //Set Tide Height
                    if (args.Length == 2)
                    {
                        float tideHeight;
                        if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out tideHeight))
                        {
                            if (closestPlanet == null)
                            {
                                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                                return;
                            }

                            foreach (var water in Waters.Values)
                            {
                                if (water.planetID == closestPlanet.EntityId)
                                {
                                    water.tideHeight = MyMath.Clamp(tideHeight, 0, 10000);
                                    MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(Waters))); RebuildLOD();
                                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetTideHeight.Replace("{0}", water.tideHeight.ToString()));
                                    return;
                                }
                            }

                            //If foreach loop doesn't find the planet, assume it doesn't exist
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                        }
                        else
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetTideHeightNoParse.Replace("{0}", args[1]));
                    }
                    break;
                case "wtidespeed":
                    sendToOthers = false;

                    if (MyAPIGateway.Session.PromoteLevel < MyPromoteLevel.Moderator)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GenericNoPermissions);
                        return;
                    }

                    //Get Tide Scale
                    if (args.Length == 1)
                    {
                        if (closestPlanet == null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                            return;
                        }

                        if (closestWater != null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GetTideSpeed.Replace("{0}", closestWater.tideSpeed.ToString()));
                            return;

                        }

                        //If foreach loop doesn't find the planet, assume it doesn't exist
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                    }

                    //Set Tide Speed
                    if (args.Length == 2)
                    {
                        float tideSpeed;
                        if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out tideSpeed))
                        {
                            if (closestPlanet == null)
                            {
                                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                                return;
                            }

                            foreach (var water in Waters.Values)
                            {
                                if (water.planetID == closestPlanet.EntityId)
                                {
                                    water.tideSpeed = MyMath.Clamp(tideSpeed, 0, 1000);
                                    MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(Waters))); RebuildLOD();
                                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetTideSpeed.Replace("{0}", water.tideSpeed.ToString()));
                                    return;
                                }
                            }

                            //If foreach loop doesn't find the planet, assume it doesn't exist
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                        }
                        else
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetTideSpeedNoParse.Replace("{0}", args[1]));
                    }
                    break;

                case "wreset":
                    sendToOthers = false;

                    if (MyAPIGateway.Session.PromoteLevel < MyPromoteLevel.Moderator)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GenericNoPermissions);
                        return;
                    }

                    if (closestPlanet == null)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                        return;
                    }

                    //Reset
                    foreach (var Key in Waters.Keys)
                    {
                        if (Waters[Key].planetID == closestPlanet.EntityId)
                        {
                            float radius = Waters[Key].radius;
                            Waters[Key] = new Water(closestPlanet);
                            Waters[Key].radius = radius;

                            MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(Waters))); RebuildLOD();
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.Reset);
                            return;
                        }
                    }

                    //If foreach loop doesn't find the planet, assume it doesn't exist
                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                    break;

                case "wwavespeed":
                    sendToOthers = false;

                    if (MyAPIGateway.Session.PromoteLevel < MyPromoteLevel.Moderator)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GenericNoPermissions);
                        return;
                    }

                    //Get Swing
                    if (args.Length == 1)
                    {
                        if (closestPlanet == null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                            return;
                        }

                        if (closestWater != null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GetWaveSpeed.Replace("{0}", closestWater.waveSpeed.ToString()));
                            return;
                        }

                        //If foreach loop doesn't find the planet, assume it doesn't exist
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                    }

                    //Set Swing
                    if (args.Length == 2)
                    {
                        float waveSpeed;
                        if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out waveSpeed))
                        {
                            if (closestPlanet == null)
                            {
                                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                                return;
                            }

                            foreach (var water in Waters.Values)
                            {
                                if (water.planetID == closestPlanet.EntityId)
                                {
                                    water.waveSpeed = MathHelper.Clamp(waveSpeed, 0f, 1f);
                                    MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(Waters))); RebuildLOD();
                                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetWaveSpeed.Replace("{0}", water.waveSpeed.ToString()));
                                    return;
                                }
                            }

                            //If foreach loop doesn't find the planet, assume it doesn't exist
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                        }
                        else
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetWaveSpeedNoParse.Replace("{0}", args[1]));
                    }
                    break;

                case "wwavescale":
                    sendToOthers = false;

                    if (MyAPIGateway.Session.PromoteLevel < MyPromoteLevel.Moderator)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GenericNoPermissions);
                        return;
                    }

                    //Get Scale
                    if (args.Length == 1)
                    {
                        if (closestPlanet == null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                            return;
                        }

                        if (closestWater != null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GetWaveScale.Replace("{0}", closestWater.waveScale.ToString()));
                            return;
                        }

                        //If foreach loop doesn't find the planet, assume it doesn't exist
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                    }

                    //Set Scale
                    if (args.Length == 2)
                    {
                        float waveScale;
                        if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out waveScale))
                        {
                            if (closestPlanet == null)
                            {
                                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                                return;
                            }

                            foreach (var water in Waters.Values)
                            {
                                if (water.planetID == closestPlanet.EntityId)
                                {
                                    water.waveScale = MathHelper.Clamp(waveScale, 0f, 25f);
                                    MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(Waters))); RebuildLOD();
                                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetWaveScale.Replace("{0}", water.waveScale.ToString()));
                                    return;
                                }
                            }

                            //If foreach loop doesn't find the planet, assume it doesn't exist
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                        }
                        else
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetWaveScaleNoParse.Replace("{0}", args[1]));
                    }
                    break;

                case "wcreate":
                    sendToOthers = false;

                    if (MyAPIGateway.Session.PromoteLevel < MyPromoteLevel.Moderator)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GenericNoPermissions);
                        return;
                    }

                    if (closestPlanet == null)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                        return;
                    }

                    if (WaterUtils.HasWater(closestPlanet))
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.HasWater);
                        return;
                    }

                    if (args.Length == 2)
                    {
                        float radius;
                        if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out radius))
                        {
                            Waters[closestPlanet.EntityId] = new Water(closestPlanet, radiusMultiplier: MathHelper.Clamp(radius, 0.01f, 2f));
                            MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(Waters))); RebuildLOD();
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.CreateWater);
                            break;
                        }
                        else
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GenericNoParse);
                            break;
                        }
                    }

                    if (args.Length == 1)
                    {
                        Waters[closestPlanet.EntityId] = new Water(closestPlanet);
                        MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(Waters))); RebuildLOD();
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.CreateWater);
                        break;
                    }

                    break;

                case "wremove":
                    sendToOthers = false;

                    if (MyAPIGateway.Session.PromoteLevel < MyPromoteLevel.Moderator)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GenericNoPermissions);
                        return;
                    }

                    if (closestPlanet == null)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                        return;
                    }

                    if (Waters.Remove(closestPlanet.EntityId))
                    {
                        MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(Waters))); RebuildLOD();
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.RemoveWater);
                        return;
                    }

                    //If foreach loop doesn't find the planet, assume it doesn't exist
                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                    break;

                case "wpdrag":
                    sendToOthers = false;

                    if (MyAPIGateway.Session.PromoteLevel < MyPromoteLevel.Moderator)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GenericNoPermissions);
                        return;
                    }

                    //Toggle Player Drag
                    if (args.Length == 1)
                    {
                        if (closestPlanet == null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                            return;
                        }

                        foreach (var water in Waters.Values)
                        {
                            if (water.planetID == closestPlanet.EntityId)
                            {
                                water.playerDrag = !water.playerDrag;
                                MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(Waters))); RebuildLOD();
                                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.TogglePlayerDrag);
                                return;
                            }
                        }

                        //If foreach loop doesn't find the planet, assume it doesn't exist
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                    }
                    break;
                case "wtransparent":
                    sendToOthers = false;

                    if (MyAPIGateway.Session.PromoteLevel < MyPromoteLevel.Moderator)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GenericNoPermissions);
                        return;
                    }

                    //Toggle Water Transparency
                    if (args.Length == 1)
                    {
                        if (closestPlanet == null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                            return;
                        }

                        foreach (var water in Waters.Values)
                        {
                            if (water.planetID == closestPlanet.EntityId)
                            {
                                water.transparent = !water.transparent;
                                MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(Waters))); RebuildLOD();
                                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.ToggleTransparency);
                                return;
                            }
                        }

                        //If foreach loop doesn't find the planet, assume it doesn't exist
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                    }
                    break;
                case "wlit":
                    sendToOthers = false;

                    if (MyAPIGateway.Session.PromoteLevel < MyPromoteLevel.Moderator)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GenericNoPermissions);
                        return;
                    }

                    //Toggle Water Lighting
                    if (args.Length == 1)
                    {
                        if (closestPlanet == null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                            return;
                        }

                        foreach (var water in Waters.Values)
                        {
                            if (water.planetID == closestPlanet.EntityId)
                            {
                                water.lit = !water.lit;
                                MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(Waters))); RebuildLOD();
                                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.ToggleLighting);
                                return;
                            }
                        }

                        //If foreach loop doesn't find the planet, assume it doesn't exist
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                    }
                    break;
                case "wtexture":
                    sendToOthers = false;

                    if (MyAPIGateway.Session.PromoteLevel < MyPromoteLevel.Moderator)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GenericNoPermissions);
                        return;
                    }

                    //Get Texture
                    if (args.Length == 1)
                    {
                        if (closestPlanet == null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                            return;
                        }

                        if (closestWater != null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GetTexture.Replace("{0}", closestWater.texture.ToString()));
                            return;
                        }

                        //If foreach loop doesn't find the planet, assume it doesn't exist
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                    }

                    //Set Texture
                    if (args.Length == 2)
                    {
                        MyStringId waterTexture = MyStringId.GetOrCompute(args[1]);
                        if (waterTexture != null)
                        {
                            if (closestPlanet == null)
                            {
                                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                                return;
                            }

                            foreach (var water in Waters.Values)
                            {
                                if (water.planetID == closestPlanet.EntityId)
                                {
                                    water.textureId = waterTexture;
                                    water.texture = args[1];
                                    water.UpdateTexture();

                                    MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(Waters))); RebuildLOD();
                                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetTexture.Replace("{0}", water.texture.ToString()));
                                    return;
                                }
                            }

                            //If foreach loop doesn't find the planet, assume it doesn't exist
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                        }
                        else
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetTextureNoFind.Replace("{0}", args[1]));
                    }
                    break;
                case "wexport":
                    sendToOthers = false;

                    if (MyAPIGateway.Session.PromoteLevel < MyPromoteLevel.Moderator)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GenericNoPermissions);
                        return;
                    }

                    if (closestPlanet == null)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                        return;
                    }

                    if (closestWater != null)
                    {
                        WaterSettings temp = new WaterSettings(closestWater);
                        MyClipboardHelper.SetClipboard(WaterUtils.ValidateXMLData(MyAPIGateway.Utilities.SerializeToXML(temp)));
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.ExportWater);
                        return;
                    }
                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                    break;

                case "wsettings":
                    sendToOthers = false;

                    if (MyAPIGateway.Session.PromoteLevel < MyPromoteLevel.Moderator)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GenericNoPermissions);
                        return;
                    }

                    if (closestPlanet == null)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                        return;
                    }

                    if (closestWater != null)
                    {
                        WaterUtils.ShowMessage(closestWater.ToString());
                        return;
                    }
                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                    break;

                case "wcrush":
                    sendToOthers = false;

                    if (MyAPIGateway.Session.PromoteLevel < MyPromoteLevel.Moderator)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GenericNoPermissions);
                        return;
                    }

                    //Get Crush Depth
                    if (args.Length == 1)
                    {
                        if (closestPlanet == null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                            return;
                        }

                        if (closestWater != null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GetCrushDepth.Replace("{0}", closestWater.crushDepth.ToString()));
                            return;
                        }

                        //If foreach loop doesn't find the planet, assume it doesn't exist
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                    }

                    //Set Crush Depth
                    if (args.Length == 2)
                    {
                        int crushDepth;
                        if (int.TryParse(WaterUtils.ValidateCommandData(args[1]), out crushDepth))
                        {
                            if (closestPlanet == null)
                            {
                                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                                return;
                            }

                            foreach (var water in Waters.Values)
                            {
                                if (water.planetID == closestPlanet.EntityId)
                                {
                                    water.crushDepth = crushDepth;
                                    MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(Waters))); RebuildLOD();
                                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetCrushDepth.Replace("{0}", water.crushDepth.ToString()));
                                    return;
                                }
                            }

                            //If foreach loop doesn't find the planet, assume it doesn't exist
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                        }
                        else
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetCrushDepthNoParse.Replace("{0}", args[1]));
                    }
                    break;
                case "wrate":
                    sendToOthers = false;

                    if (MyAPIGateway.Session.PromoteLevel < MyPromoteLevel.Moderator)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GenericNoPermissions);
                        return;
                    }

                    //Get Collection Rate
                    if (args.Length == 1)
                    {
                        if (closestPlanet == null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                            return;
                        }

                        if (closestWater != null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GetCollectRate.Replace("{0}", closestWater.collectionRate.ToString()));
                            return;
                        }

                        //If foreach loop doesn't find the planet, assume it doesn't exist
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                    }

                    //Set Collect Rate
                    if (args.Length == 2)
                    {
                        float collectRate;
                        if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out collectRate))
                        {
                            if (closestPlanet == null)
                            {
                                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                                return;
                            }

                            foreach (var water in Waters.Values)
                            {
                                if (water.planetID == closestPlanet.EntityId)
                                {
                                    water.collectionRate = collectRate;
                                    MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(Waters))); RebuildLOD();
                                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetCollectRate.Replace("{0}", water.collectionRate.ToString()));
                                    return;
                                }
                            }

                            //If foreach loop doesn't find the planet, assume it doesn't exist
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                        }
                        else
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetCollectRateNoParse.Replace("{0}", args[1]));
                    }
                    break;
                case "wcolor":
                    sendToOthers = false;

                    if (MyAPIGateway.Session.PromoteLevel < MyPromoteLevel.Moderator)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GenericNoPermissions);
                        return;
                    }

                    //Get Fog Color
                    if (args.Length == 1)
                    {
                        if (closestPlanet == null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                            return;
                        }

                        if (closestWater != null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GetFogColor.Replace("{0}", closestWater.fogColor.ToString()));
                            return;
                        }

                        //If foreach loop doesn't find the planet, assume it doesn't exist
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                    }

                    //Set Fog Color
                    if (args.Length == 4)
                    {
                        double r;
                        double g;
                        double b;

                        if (double.TryParse(WaterUtils.ValidateCommandData(args[1]), out r))
                        {
                            if (double.TryParse(WaterUtils.ValidateCommandData(args[2]), out g))
                            {
                                if (double.TryParse(WaterUtils.ValidateCommandData(args[3]), out b))
                                {
                                    if (closestPlanet == null)
                                    {
                                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                                        return;
                                    }

                                    foreach (var water in Waters.Values)
                                    {
                                        if (water.planetID == closestPlanet.EntityId)
                                        {
                                            water.fogColor = new Vector3D(r, g, b);

                                            if (water.fogColor.Max() > 1)
                                                water.fogColor /= 255f;

                                            MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(Waters))); RebuildLOD();
                                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetFogColor.Replace("{0}", water.fogColor.ToString()));
                                            return;
                                        }
                                    }

                                    //If foreach loop doesn't find the planet, assume it doesn't exist
                                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                                }
                                else
                                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetFogColorNoParse.Replace("{0}", args[3]));
                            }
                            else
                                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetFogColorNoParse.Replace("{0}", args[2]));
                        }
                        else
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetFogColorNoParse.Replace("{0}", args[1]));
                    }

                    break;
            }
        }

        /// <summary>
        /// Called when the world is loading
        /// </summary>
        public override void LoadData()
        {
            WeaponCoreAPI.Load();

            string packet;
            MyAPIGateway.Utilities.GetVariable("JWater", out packet);
            if (packet == null)
                return;

            if (packet.Contains("Dictionary"))
            {
                SerializableDictionary<long, Water> tempDictWaters = MyAPIGateway.Utilities.SerializeFromXML<SerializableDictionary<long, Water>>(packet);

                if (tempDictWaters != null)
                {
                    Waters = tempDictWaters.Dictionary;
                }
            }
            else
            {
                List<Water> tempWaters = MyAPIGateway.Utilities.SerializeFromXML<List<Water>>(packet);

                if (tempWaters != null)
                    foreach (var water in tempWaters)
                    {
                        Waters[water.planetID] = water;
                    }
            }

            /*WaterUtils.WriteLog("Loading Water");
            for (int i = waters.Count - 1; i >= 0; i--)
            {
                for (int j = 0; j < waters.Count; j++)
                {
                    if (i != j && waters[i].planetID == waters[j].planetID)
                    {
                        WaterUtils.WriteLog("Found duplicate water, removing");
                        waters.RemoveAtFast(j);
                        break;
                    }
                }
            }*/

            foreach (var water in Waters.Values)
            {
                water.UpdateTexture();
            }

            LoadSettings();

            //Draygo
            DragAPI.Init();
        }

        //spaghetti
        public void LoadSettings()
        {
            //this is spaghetti
            if (!MyAPIGateway.Utilities.IsDedicated)
            {
                if (MyAPIGateway.Utilities.FileExistsInGlobalStorage("JakariaWaterSettings.cfg"))
                {
                    TextReader file = MyAPIGateway.Utilities.ReadFileInGlobalStorage("JakariaWaterSettings.cfg");
                    string line;

                    Settings.Quality = float.Parse(file.ReadLine());

                    Language language;
                    line = file.ReadLine();
                    if (line != null && WaterLocalization.Languages.TryGetValue(line, out language))
                        WaterLocalization.CurrentLanguage = language;
                    else
                        WaterLocalization.CurrentLanguage = WaterLocalization.Languages.TryGetValue(MyAPIGateway.Session.Config.Language.ToString().ToLower(), out language) ? language : WaterLocalization.Languages.GetValueOrDefault("english");

                    bool showDepthSetting;
                    if (bool.TryParse(file.ReadLine(), out showDepthSetting))
                    {
                        Settings.ShowDepth = showDepthSetting;
                    }

                    float volumeMultiplierSetting;
                    if (float.TryParse(file.ReadLine(), out volumeMultiplierSetting))
                    {
                        Settings.Volume = volumeMultiplierSetting;
                    }

                    bool showAltitudeSetting;
                    if (bool.TryParse(file.ReadLine(), out showAltitudeSetting))
                    {
                        Settings.ShowAltitude = showAltitudeSetting && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator && MyAPIGateway.Session.CreativeMode;
                    }

                    file.Close();
                }
            }
        }

        //Also spaghetti
        public void SaveSettings()
        {
            if (!MyAPIGateway.Utilities.IsDedicated)
            {
                System.IO.TextWriter writer = MyAPIGateway.Utilities.WriteFileInGlobalStorage("JakariaWaterSettings.cfg");
                writer.WriteLine(Settings.Quality);
                writer.WriteLine(WaterLocalization.CurrentLanguage.EnglishName);
                writer.WriteLine(Settings.ShowDepth);
                writer.WriteLine(Settings.Volume);
                writer.WriteLine(Settings.ShowAltitude);
                writer.Close();
            }
        }

        /// <summary>
        /// Called when the world is saving
        /// </summary>
        public override void SaveData()
        {
            MyAPIGateway.Utilities.SetVariable("JWater", MyAPIGateway.Utilities.SerializeToXML(new SerializableDictionary<long, Water>(Waters)));
        }

        /// <summary>
        /// Called when the world is unloading
        /// </summary>
        protected override void UnloadData()
        {
            WeaponCoreAPI.Unload();

            //Draygo
            DragAPI.Close();

            MyAPIGateway.Multiplayer.UnregisterMessageHandler(WaterData.ClientHandlerID, ClientHandler);
            MyAPIGateway.Utilities.MessageEntered -= Utilities_MessageEntered;
            //MyExplosions.OnExplosion -= MyExplosions_OnExplosion;
            if (MyAPIGateway.Session.IsServer)
            {
                MyVisualScriptLogicProvider.PlayerConnected -= PlayerConnected;
                MyVisualScriptLogicProvider.PlayerConnected -= PlayerDisconnected;
                MyEntities.OnEntityAdd -= Entities_OnEntityAdd;
                MyVisualScriptLogicProvider.RespawnShipSpawned -= RespawnShipSpawned;
                MyVisualScriptLogicProvider.PrefabSpawnedDetailed -= PrefabSpawnedDetailed;
            }

            EnvironmentUnderwaterSoundEmitter.Cleanup();
            EnvironmentUndergroundSoundEmitter.Cleanup();
            EnvironmentOceanSoundEmitter.Cleanup();
            EnvironmentBeachSoundEmitter.Cleanup();
            AmbientSoundEmitter.Cleanup();
            AmbientBoatSoundEmitter.Cleanup();
        }

        private void RespawnShipSpawned(long shipEntityId, long playerId, string respawnShipPrefabName)
        {
            IMyCubeGrid Grid = MyEntities.GetEntityById(shipEntityId) as IMyCubeGrid;

            if (Grid == null)
                return;

            float spawnAltitude = 2000;

            List<IMySlimBlock> Blocks = new List<IMySlimBlock>();
            Grid.GetBlocks(Blocks);

            int TankCount = 0;
            foreach (var Block in Blocks)
            {
                if (Block is IMyGasTank)
                    TankCount++;
            }

            bool TargetOverwater = TankCount > 1;

            Vector3D position = Grid.GetPosition();
            MyPlanet planet = MyGamePruningStructure.GetClosestPlanet(position);

            if (planet != null)
            {
                Water water;
                if (Waters.TryGetValue(planet.EntityId, out water))
                {
                    MyAPIGateway.Parallel.Start(() =>
                    {
                        for (int i = 0; i < 1000; i++)
                        {
                            Vector3D surfacePoint = planet.GetClosestSurfacePointGlobal(planet.PositionComp.GetPosition() + (MyUtils.GetRandomVector3Normalized() * (water.radius + 10)));

                            bool Underwater = water.IsUnderwater(ref surfacePoint, -10);
                            if (Underwater)
                            {
                                if (TargetOverwater)
                                {
                                    Vector3D up = water.GetUpDirection(surfacePoint);
                                    MatrixD matrix = MatrixD.CreateWorld(surfacePoint + (up * spawnAltitude), Vector3D.CalculatePerpendicularVector(up), up);

                                    MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                                    {
                                        Grid.Physics.LinearVelocity = Vector3D.Zero;
                                        Grid.PositionComp.SetWorldMatrix(ref matrix);
                                    });
                                    return;
                                }
                            }
                            else
                            {
                                if (!TargetOverwater)
                                {
                                    Vector3D up = water.GetUpDirection(surfacePoint);
                                    MatrixD matrix = MatrixD.CreateWorld(surfacePoint + (up * spawnAltitude), Vector3D.CalculatePerpendicularVector(up), up);

                                    MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                                    {
                                        Grid.Physics.LinearVelocity = Vector3D.Zero;
                                        Grid.PositionComp.SetWorldMatrix(ref matrix);
                                    });

                                    return;
                                }
                            }
                        }
                    });
                }
            }

        }

        /// <summary>
        /// Called once every tick after the simulation has run
        /// </summary>
        public override void UpdateAfterSimulation()
        {
            GeneralNoiseTimer += 0.01;
            tickTimerSecond++;
            if (tickTimerSecond > 60)
            {
                UpdateAfterSecond();
                tickTimerSecond = 0;
            }

            // Text Hud API Update
            if (!MyAPIGateway.Utilities.IsDedicated && TextAPI.Heartbeat)
            {
                DepthMeter.Visible = Settings.ShowDepth && (Session.CameraDepth < 0 || (Settings.ShowAltitude && ((MyAPIGateway.Session.Player?.Controller?.ControlledEntity is IMyCharacter) == false && Session.CameraDepth < 100)));

                if (Session.CameraDepth > 100)
                    DepthMeter.Visible = false;
            }

            tickTimer++;
            //Delayed updating
            if (tickTimer > tickTimerMax)
            {
                tickTimer = 0;

                if (Waters == null)
                    return;

                //Server and Client

                for (int i = Waters.Keys.Count - 1; i >= 0; i--)
                {
                    long Key = Waters.Keys.ToList()[i];
                    if (!MyEntities.EntityExists(Key))
                    {
                        Waters.Remove(Key);
                        continue;
                    }

                    if (Waters[Key].planet == null)
                        Waters[Key].planet = (MyPlanet)MyEntities.GetEntityById(Key);
                }

                //Server
                if (MyAPIGateway.Session.IsServer)
                {
                    List<MyObjectBuilder_WeatherPlanetData> weatherPlanets = weatherEffects.GetWeatherPlanetData();

                    foreach (var water in Waters.Values)
                    {
                        //Update Underwater Entities
                        BoundingSphereD sphere = new BoundingSphereD(water.position, water.currentRadius + water.waveHeight + water.tideHeight);
                        water.underWaterEntities.Clear();
                        MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref sphere, water.underWaterEntities, MyEntityQueryType.Dynamic);

                        /*//Remove Underwater Weathers
                        if (weatherPlanets?.Count != 0)
                            for (int i = weatherPlanets.Count - 1; i >= 0; i--)
                            {
                                if (water.planetID == weatherPlanets[i].PlanetId)
                                {
                                    for (int j = weatherPlanets[i].Weathers.Count - 1; j >= 0; j--)
                                    {
                                        if (water.IsUnderwaterSquared(weatherPlanets[i].Weathers[j].Position))
                                            weatherEffects.RemoveWeather(weatherPlanets[i].Weathers[j]);
                                    }
                                }
                            }*/
                    }
                }

                //Client
                if (!MyAPIGateway.Utilities.IsDedicated)
                {
                    if (closestPlanet != null)
                        nightValue = MyMath.Clamp(Vector3.Dot(-Session.GravityDirection, WaterMod.Session.SunDirection) + 0.22f, 0.22f, 1f);

                    Session.SunDirection = MyVisualScriptLogicProvider.GetSunDirection();

                    if (closestWater != null && closestPlanet != null)
                    {
                        if (TextAPI.Heartbeat && DepthMeter.Visible)
                        {
                            string message;
                            if (Session.CameraDepth < -closestWater.waveHeight * 2)
                                message = WaterLocalization.CurrentLanguage.Depth.Replace("{0}", Math.Round(-closestWater.GetDepthSimple(Session.CameraPosition)).ToString());
                            else if (Session.CameraDepth < 0)
                                message = WaterLocalization.CurrentLanguage.Depth.Replace("{0}", Math.Round(-Session.CameraDepth).ToString());
                            else
                                message = WaterLocalization.CurrentLanguage.Altitude.Replace("{0}", Math.Round(Session.CameraDepth).ToString());

                            if (Session.CameraDepth < -closestWater.crushDepth)
                                message = "- " + message + " -";

                            DepthMeter.Message = new StringBuilder("\n\n\n" + message);

                            if (!Session.CameraAirtight)
                                DepthMeter.InitialColor = Color.Lerp(Color.Lerp(Color.White, Color.Yellow, MathHelper.Clamp(-Session.CameraDepth / closestWater.crushDepth, 0f, 1f)), Color.Red, -(Session.CameraDepth + (closestWater.crushDepth - 100)) / 100);
                            else
                                DepthMeter.InitialColor = Color.Lerp(DepthMeter.InitialColor, Color.White, 0.25f);

                            DepthMeter.Offset = new Vector2D(-DepthMeter.GetTextLength().X / 2, 0);
                        }

                        weatherEffects.GetWeather(Session.CameraPosition, out closestWeather);

                        BoundingSphereD sphere = new BoundingSphereD(Session.CameraPosition, 1000);
                        nearbyEntities.Clear();
                        MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref sphere, nearbyEntities);

                        /*if (closestWeather != null && (closestWeather.Weather.StartsWith("Rain") || closestWeather.Weather.StartsWith("ThunderStorm")))
                        {
                            if (weatherEffects.GetWeatherIntensity(Session.CameraPosition, closestWeather) > 0.1f)
                                for (int i = 0; i < (int)(Settings.Quality * 20); i++)
                                {
                                    Splashes.Add(new Splash(closestWater.GetClosestSurfacePoint(Session.CameraPosition + (MyUtils.GetRandomVector3Normalized() * 50)), 0.5f));
                                }
                        }*/

                        if (nearbyEntities != null)
                            foreach (var entity in nearbyEntities)
                            {
                                if (entity == null || entity.Physics == null || !entity.Physics.Enabled)
                                    continue;

                                float capacity = 0;
                                Vector3D center = Vector3.Zero;
                                float litres = 0;

                                Vector3D velocity = entity.Physics.LinearVelocity;
                                Vector3D verticalVelocity = Vector3D.ProjectOnVector(ref velocity, ref Session.GravityDirection);
                                Vector3D horizontalVelocity = velocity - verticalVelocity;

                                if (entity is MyCubeGrid)
                                {
                                    ListReader<FatBlockStorage.BlockStorage> fatBlocks;
                                    if (FatBlockStorage.Storage.TryGetValue(entity.EntityId, out fatBlocks))
                                    {
                                        foreach (var blockStorage in fatBlocks)
                                        {
                                            if (blockStorage == null)
                                                continue;

                                            MyCubeBlock block = blockStorage.Block;

                                            if (block == null || block.MarkedForClose || block.Closed)
                                                continue;

                                            Vector3D BlockPosition = block.PositionComp.GetPosition();
                                            bool belowWater = closestWater.IsUnderwater(ref BlockPosition);

                                            if (belowWater != blockStorage.PreviousUnderwaterClient)
                                            {
                                                blockStorage.PreviousUnderwaterClient = belowWater;

                                                Vector3 PointVelocity = block.CubeGrid.Physics.GetVelocityAtPoint(BlockPosition);
                                                float Velocity = PointVelocity.Length();
                                                float UpVelocity = Math.Abs((PointVelocity * -((Vector3)Session.GravityDirection)).Length());

                                                if (UpVelocity > 4)
                                                    if (belowWater)
                                                    {
                                                        //Enter Underwater
                                                        Splashes.Add(new Splash(BlockPosition, block.CubeGrid.GridSize * (Velocity / 10f)));
                                                    }
                                                    else
                                                    {
                                                        //PointVelocity * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS

                                                        SimulatedSplashes.Add(new SimulatedSplash(BlockPosition, PointVelocity * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS + (MyUtils.GetRandomVector3Normalized() * 0.05f), block.CubeGrid.GridSize * 1.5f));

                                                        //Two splashes created for large grid
                                                        if (block.CubeGrid.GridSizeEnum == MyCubeSize.Large)
                                                        {
                                                            SimulatedSplashes.Add(new SimulatedSplash(BlockPosition, PointVelocity * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS + (MyUtils.GetRandomVector3Normalized() * 0.05f), block.CubeGrid.GridSize));
                                                        }

                                                        Splashes.Add(new Splash(BlockPosition, block.CubeGrid.GridSize * (Velocity / 10f)));
                                                        //Exit Underwater
                                                    }
                                            }

                                            if (!block.IsFunctional)
                                            {
                                                if (block.IsBuilt && belowWater)
                                                {
                                                    block.StopDamageEffect(true);
                                                    if (MyUtils.GetRandomInt(0, 100) < 7)
                                                        lock (effectLock)
                                                            Bubbles.Add(new Bubble(BlockPosition, block.CubeGrid.GridSizeHalf));
                                                }
                                                continue;
                                            }

                                            if (belowWater)
                                            {
                                                if (Settings.ShowCenterOfBuoyancy)
                                                {
                                                    if (block is IMyGasTank)
                                                    {
                                                        IMyGasTank gasTank = block as IMyGasTank;

                                                        capacity = (float)gasTank.FilledRatio * gasTank.Capacity;
                                                        litres += capacity;
                                                        center += block.Position * capacity;
                                                        continue;
                                                    }

                                                    if (block is IMyWheel)
                                                    {
                                                        if (!block.IsFunctional)
                                                            continue;

                                                        if (block.CubeGrid.GridSizeEnum == MyCubeSize.Large)
                                                            capacity = block.BlockDefinition.Mass * 31;
                                                        else
                                                            capacity = block.BlockDefinition.Mass * 25;

                                                        litres += capacity;
                                                        center += block.Position * capacity;

                                                        continue;
                                                    }
                                                }

                                                if (block is MyThrust)
                                                {
                                                    MyThrust thruster = block as MyThrust;

                                                    if (thruster == null || !thruster.IsPowered || Vector3D.Distance(closestWater.position, thruster.PositionComp.GetPosition()) > closestWater.currentRadius)
                                                        continue;

                                                    if (thruster.BlockDefinition.ThrusterType.String.Equals("Atmospheric") && thruster.CurrentStrength >= MyUtils.GetRandomFloat(0f, 1f) && MyUtils.GetRandomInt(0, 2) == 1)
                                                    {
                                                        Bubbles.Add(new Bubble(thruster.PositionComp.GetPosition(), thruster.CubeGrid.GridSize));
                                                    }

                                                    continue;
                                                }

                                                if (block is IMyShipDrill)
                                                {
                                                    if (block.IsWorking && MyUtils.GetRandomInt(0, 100) < 10)
                                                        lock (effectLock)
                                                            Bubbles.Add(new Bubble(block.PositionComp.GetPosition(), block.CubeGrid.GridSizeHalf));
                                                    continue;
                                                }
                                            }
                                        }

                                        Vector3D centerOfBuoyancy = (entity as MyCubeGrid).GridIntegerToWorld(center / litres);

                                        if (Settings.ShowCenterOfBuoyancy && centerOfBuoyancy.IsValid())
                                            indicators.Add(new COBIndicator(centerOfBuoyancy, (entity as MyCubeGrid).GridSizeEnum));

                                        float depth = Vector3.Distance(closestWater.position, centerOfBuoyancy) - closestWater.currentRadius;
                                        float horizontalVelocityLength = (float)horizontalVelocity.Length();
                                    }
                                    continue;
                                }

                                if (entity is IMyCharacter)
                                {
                                    Vector3D position = (entity as IMyCharacter).GetHeadMatrix(false, false, false, false).Translation;
                                    if ((closestWater.GetDepth(ref position) > 0 || WaterUtils.IsPositionAirtight(position)) && WaterUtils.HotTubUnderwater(entity.PositionComp.GetPosition()) && verticalVelocity.Length() > 2f)
                                    {
                                        Splashes.Add(new Splash(entity.PositionComp.GetPosition(), Math.Min(entity.Physics.Speed, 2f)));
                                    }
                                }

                                Vector3D EntityPosition = entity.PositionComp.GetPosition();
                                if (closestWater.IsUnderwater(ref EntityPosition))
                                {
                                    if (entity is MyFloatingObject)
                                    {
                                        if (MyUtils.GetRandomFloat(0, 1) < 0.12f && !WaterUtils.IsPositionAirtight(EntityPosition))
                                            CreateBubble(entity.PositionComp.GetPosition(), 0.4f);

                                        continue;
                                    }

                                    if (entity is IMyCharacter)
                                    {
                                        Vector3D position = (entity as IMyCharacter).GetHeadMatrix(false, false, false, false).Translation;

                                        float Depth = closestWater.GetDepth(ref position);

                                        if (Depth < 1 && Depth > -1 && verticalVelocity.Length() > 2f)
                                        {
                                            Splashes.Add(new Splash(closestWater.GetClosestSurfacePoint(ref position), entity.Physics.Speed));
                                        }

                                        if (Depth < 0 && MyUtils.GetRandomFloat(0, 1) < 0.12f && !WaterUtils.IsPositionAirtight(position))
                                            CreateBubble(position, 0.2f);

                                        continue;
                                    }
                                }
                            }
                    }
                }

                /*if (MyAPIGateway.Input.IsGameControlPressed(MyControlsSpace.JUMP))
                {
                    MyAPIGateway.Session.Player.Character.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, Vector3D.Normalize(MyAPIGateway.Session.Player.Character.Physics.Gravity) * -50000, null, null);
                }*/
            }

            SimulatePhysics();
            Session.SessionTimer++;

            //Clients
            if (!MyAPIGateway.Utilities.IsDedicated && initialized && MyAPIGateway.Session != null && MyAPIGateway.Session.Player != null && MyAPIGateway.Session.Camera != null)
            {

                if (Settings.Volume != 0)
                    SimulateSounds();

                Session.InsideGrid = MyAPIGateway.Session?.Player?.Character?.Components?.Get<MyEntityReverbDetectorComponent>() != null ? MyAPIGateway.Session.Player.Character.Components.Get<MyEntityReverbDetectorComponent>().Grids : 0;
                Session.InsideVoxel = MyAPIGateway.Session?.Player?.Character?.Components?.Get<MyEntityReverbDetectorComponent>() != null ? MyAPIGateway.Session.Player.Character.Components.Get<MyEntityReverbDetectorComponent>().Voxels : 0;

                Session.CameraAirtight = WaterUtils.IsPositionAirtight(MyAPIGateway.Session.Camera.Position);

                if (Session.CameraAirtight || ((MyAPIGateway.Session.Player.Controller?.ControlledEntity?.Entity as MyCockpit) != null && (MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity as MyCockpit).OxygenFillLevel > 0f))
                    Session.InsideGrid = 25;

                if (MyAPIGateway.Input.IsNewGameControlPressed(MyControlsSpace.TOGGLE_HUD))
                    Settings.ShowHud = !MyAPIGateway.Session?.Config?.MinimalHud ?? true;

                closestPlanet = MyGamePruningStructure.GetClosestPlanet(Session.CameraPosition);
                closestWater = GetClosestWater(Session.CameraPosition);

                if (closestPlanet != null)
                {
                    Session.GravityDirection = Vector3D.Normalize(closestPlanet.PositionComp.GetPosition() - Session.CameraPosition);

                    Session.Gravity = Session.GravityDirection * closestPlanet.Generator.SurfaceGravity;
                }

                if (closestWater != null)
                    MyAPIGateway.Parallel.StartBackground(SimulateEffects);

                DrawAfterSimulation();
            }

        }

        void RebuildLOD()
        {
            Session.LastLODBuildPosition = Session.CameraPosition;

            foreach (var water in Waters.Values)
            {
                if (water.waterFaces == null)
                {
                    water.waterFaces = new WaterFace[WaterData.Directions.Length];
                }

                for (int i = 0; i < water.waterFaces.Length; i++)
                {
                    if (water.waterFaces[i] == null)
                        water.waterFaces[i] = new WaterFace(water, WaterData.Directions[i]);

                    water.waterFaces[i].ConstructTree();
                }
            }
        }

        private void DrawAfterSimulation()
        {
            Vector4 WhiteColor = new Vector4(nightValue, nightValue, nightValue, 1);
            QuadBillboards.Clear();

            //Hot Tubs
            foreach (var Tub in FatBlockStorage.HotTubs)
            {
                if (Tub == null)
                    continue;

                if (Tub.underWater && !Tub.airtight)
                    continue;

                MatrixD Matrix = Tub.Block.PositionComp.WorldMatrixRef;
                Vector4 Color = WaterData.WaterColor * MyMath.Clamp(Vector3.Dot(Matrix.Up, Session.SunDirection) + 0.1f, 0.05f, 1f);

                Color.W = WaterData.WaterColor.W;

                if (Session.CameraHottub)
                    Color *= 0.5f;

                float GridSize = Tub.Block.CubeGrid.GridSize;
                Vector3D Min = Tub.Block.Min;
                Vector3D Max = Tub.Block.Max;
                double RightSize = Math.Abs(Max.X - Min.X);
                double ForwardSize = Math.Abs(Max.Z - Min.Z);
                float HeightOffset = -(GridSize / 2) + (((float)Tub.inventory.CurrentVolume / (float)Tub.inventory.MaxVolume) * (GridSize / 2));

                Vector3D Corner = Tub.Block.PositionComp.GetPosition() - (Matrix.Right * (RightSize * GridSize - 0.1)) - (Matrix.Forward * (ForwardSize * GridSize - 0.1));
                Vector3D Right = ((Matrix.Right * (RightSize * GridSize - 0.2)) / 3) * 2;
                Vector3D Forward = ((Matrix.Forward * (ForwardSize * GridSize - 0.2)) / 3) * 2;
                Vector3D Up = Matrix.Up;

                MyQuadD Quad = new MyQuadD();
                for (int z = 0; z < RightSize * 3; z++)
                {
                    for (int x = 0; x < ForwardSize * 3; x++)
                    {
                        Quad.Point0 = Corner + (((z * Right) + (x * Forward)));
                        Quad.Point1 = Corner + (((z * Right) + ((x + 1) * Forward)));
                        Quad.Point2 = Corner + ((((z + 1) * Right) + ((x + 1) * Forward)));
                        Quad.Point3 = Corner + ((((z + 1) * Right) + (x * Forward)));

                        Quad.Point0 += (((GeneralNoise.GetNoise((Quad.Point0.X + GeneralNoiseTimer) * 50, (Quad.Point0.Y + GeneralNoiseTimer) * 50, (Quad.Point0.Z + GeneralNoiseTimer) * 50) * 0.03) + HeightOffset) * Up);
                        Quad.Point1 += (((GeneralNoise.GetNoise((Quad.Point1.X + GeneralNoiseTimer) * 50, (Quad.Point1.Y + GeneralNoiseTimer) * 50, (Quad.Point1.Z + GeneralNoiseTimer) * 50) * 0.03) + HeightOffset) * Up);
                        Quad.Point2 += (((GeneralNoise.GetNoise((Quad.Point2.X + GeneralNoiseTimer) * 50, (Quad.Point2.Y + GeneralNoiseTimer) * 50, (Quad.Point2.Z + GeneralNoiseTimer) * 50) * 0.03) + HeightOffset) * Up);
                        Quad.Point3 += (((GeneralNoise.GetNoise((Quad.Point3.X + GeneralNoiseTimer) * 50, (Quad.Point3.Y + GeneralNoiseTimer) * 50, (Quad.Point3.Z + GeneralNoiseTimer) * 50) * 0.03) + HeightOffset) * Up);

                        QuadBillboards.Push(new QuadBillboard(WaterData.HotTubMaterial, ref Quad, ref Color));
                    }
                }
            }

            //Water Surface
            //MyAPIGateway.Parallel.Do(() =>
            //{   

            MyAPIGateway.Parallel.ForEach(Waters.Values, water =>
            {
                if (water?.waterFaces != null)
                    foreach (var face in water.waterFaces)
                    {
                        if (face?.water == null)
                            continue;
                        face.Draw(water.planetID == closestWater?.planetID && closestPlanet != null);
                    }
            });
            //});

            //Effects
            lock (effectLock)
            {
                float lifeRatio = 0;
                Vector4 Color = Vector4.One;// new Vector4(nightValue, nightValue, nightValue, 1);

                if (!Session.CameraUnderwater)
                {
                    foreach (var splash in Splashes)
                    {
                        if (splash == null)
                            continue;

                        lifeRatio = (float)splash.life / splash.maxLife;
                        float size = splash.radius * lifeRatio;
                        MyQuadD Quad = new MyQuadD();
                        Vector3D axisA = Session.GravityAxisA * size;
                        Vector3D axisB = Session.GravityAxisB * size;
                        Quad.Point0 = closestWater.GetClosestSurfacePoint(splash.position - axisA - axisB);
                        Quad.Point2 = closestWater.GetClosestSurfacePoint(splash.position + axisA + axisB);
                        Quad.Point1 = closestWater.GetClosestSurfacePoint(splash.position - axisA + axisB);
                        Quad.Point3 = closestWater.GetClosestSurfacePoint(splash.position + axisA - axisB);

                        QuadBillboards.Push(new QuadBillboard(WaterData.SplashMaterial, ref Quad, WhiteColor * (1f - lifeRatio)));
                    }
                }
            }
        }

        /// <summary>
        /// Called once every second (60 ticks) after the simulation has run
        /// </summary>
        private void UpdateAfterSecond()
        {
            try
            {
                players.Clear();
                MyAPIGateway.Players.GetPlayers(players);

                //Clients
                if (!MyAPIGateway.Utilities.IsDedicated)
                {
                    if (Settings.ShowDebug && MyAPIGateway.Session?.Player?.Character != null)
                    {
                        MyAPIGateway.Utilities.ShowNotification("" + WaterData.DotMaxFOV, 1000);
                    }

                    Session.CameraClosestPosition = closestPlanet?.GetClosestSurfacePointGlobal(Session.CameraPosition) ?? Vector3D.Zero;
                    Session.CameraUnderground = closestPlanet?.IsUnderGround(Session.CameraPosition) ?? false;
                    Session.CameraAboveWater = closestWater == null || Session.CameraDepth > 0;
                }
            }
            catch (Exception e)
            {
                //Keen crash REEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEE
                MyLog.Default.WriteLine(e);
                WaterUtils.ShowMessage(e.ToString());
            }
        }

        /// <summary>
        /// Creates a bubble
        /// </summary>
        public void CreateBubble(Vector3 position, float radius)
        {
            Bubbles.Add(new Bubble(position, radius));
        }

        public void CreateSplash(Vector3D Position, float Radius, bool Audible)
        {
            Splashes.Add(new Splash(Position, Radius, Audible ? 1 : 0));
        }

        public void CreatePhysicsSplash(Vector3D Position, Vector3D Velocity, float Radius, int Count = 1)
        {
            for (int i = 0; i < Count; i++)
            {
                SimulatedSplashes.Add(new SimulatedSplash(Position, (Velocity + MyUtils.GetRandomVector3HemisphereNormalized(Vector3D.Normalize(Velocity)) * 0.5f) / 3, Radius));
            }
        }

        /// <summary>
        /// Simulates water physics
        /// </summary>
        public void SimulatePhysics()
        {
            foreach (var water in Waters.Values)
            {
                water.waveTimer += water.waveSpeed * MyAPIGateway.Physics.ServerSimulationRatio;
                water.tideTimer += (water.tideSpeed / 1000f) * MyAPIGateway.Physics.ServerSimulationRatio;
                water.tideDirection = new Vector3D(Math.Cos(water.tideTimer), 0, Math.Sin(water.tideTimer));
                water.currentRadius = water.radius;
            }

            if (MyAPIGateway.Session.IsServer)
            {
                foreach (var Block in DamageQueue)
                {
                    if (Block.Item1?.SlimBlock != null && (Block.Item1).MarkedForClose == false)
                        ((IMySlimBlock)(Block.Item1.SlimBlock))?.DoDamage(Block.Item2, MyDamageType.Fall, true);
                }
                DamageQueue.Clear();

                foreach (var water in Waters.Values)
                {
                    if (water.viscosity == 0 && water.buoyancy == 0 || water.fluidDensity == 0)
                        continue;

                    foreach (var entity in water.underWaterEntities)
                    {
                        if (entity == null || entity.Physics == null || entity.Physics.IsStatic || entity.MarkedForClose || entity.IsPreview)
                            continue;

                        if (entity is MyCubeGrid)
                        {
                            simulateGridThreads.Add(new SimulateGridThread(water, entity as MyCubeGrid));
                            continue;
                        }

                        if (entity is MyFloatingObject)
                        {

                            if (!WaterUtils.IsPositionAirtight(entity.PositionComp.GetPosition()))
                            {
                                if ((entity as MyFloatingObject).Item.Content.SubtypeName == WaterData.IceItem.SubtypeName)
                                {
                                    entity.Close();
                                }

                                if (!entity.MarkedForClose)
                                    entity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, -entity.Physics.LinearVelocity * water.viscosity * entity.Physics.Mass * 14, null, null);
                            }

                            continue;
                        }

                        if (entity is MyMeteor)
                        {
                            MyVisualScriptLogicProvider.CreateExplosion(entity.PositionComp.GetPosition(), 2, 0);
                            MyFloatingObjects.Spawn(new MyPhysicalInventoryItem(500 * (MyFixedPoint)MyUtils.GetRandomFloat(1f, 3f), MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Ore>(WaterUtils.GetRandomMeteorMaterial())), entity.WorldMatrix, null, null);
                            entity.Close();
                        }
                    }
                    simulateGridThreads.ApplyAdditions();
                    MyAPIGateway.Parallel.For(0, simulateGridThreads.Count, i =>
                    {
                        SimulateGrid(simulateGridThreads[i].Water, simulateGridThreads[i].Grid);
                    }, 10);
                    simulateGridThreads.ClearImmediate();

                    tickTimerPlayerDamage++;
                    if (tickTimerPlayerDamage >= 60)
                        tickTimerPlayerDamage = 0;

                    if (water.playerDrag)
                    {
                        //Character Drag
                        if (players != null)
                            foreach (var player in players)
                            {
                                if (player?.Character?.Physics == null)
                                    continue;

                                simulatePlayerThreads.Add(new SimulatePlayerThread(water, player.Character));
                            }

                        simulatePlayerThreads.ApplyAdditions();
                        MyAPIGateway.Parallel.For(0, simulatePlayerThreads.Count, i =>
                        {
                            SimulatePlayer(simulatePlayerThreads[i].Water, (simulatePlayerThreads[i].Character));
                        }, 10);
                        simulatePlayerThreads.ClearImmediate();
                    }
                }
            }

            //Clients, no host
            if (!MyAPIGateway.Session.IsServer)
            {
                if (closestWater != null)
                {
                    if (MyAPIGateway.Session.Player?.Character != null)
                    {
                        IMyCharacter character = MyAPIGateway.Session.Player.Character;
                        SimulatePlayer(closestWater, MyAPIGateway.Session.Player.Character);

                        if (WaterUtils.HotTubUnderwater(character.GetPosition()))
                        {
                            Vector3 characterforce = -character.Physics.LinearVelocity * 0.1f * character.Physics.Mass * 12;

                            if ((!character.EnabledThrusts || !character.EnabledDamping) && WaterUtils.IsPlayerStateFloating(character.CurrentMovementState))
                                characterforce += character.Physics.Mass * -character.Physics.Gravity * 1.2f;

                            if (characterforce.IsValid())
                                character.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, characterforce, null, null);
                        }
                    }

                    if (MyAPIGateway.Session.ControlledObject?.Entity is MyShipController)
                    {
                        SimulateGrid(closestWater, (MyAPIGateway.Session.ControlledObject as MyShipController).CubeGrid);
                        //(MyAPIGateway.Session.ControlledObject as MyCockpit).CubeGrid.ForceDisablePrediction = true;
                    }
                }
            }
        }

        struct SimulateGridThread
        {
            public MyCubeGrid Grid { get; }
            public Water Water { get; }

            public SimulateGridThread(Water water, MyCubeGrid grid)
            {
                this.Grid = grid;
                this.Water = water;
            }
        }

        struct SimulatePlayerThread
        {
            public IMyCharacter Character { get; }
            public Water Water { get; }

            public SimulatePlayerThread(Water water, IMyCharacter character)
            {
                this.Character = character;
                this.Water = water;
            }
        }

        /// <summary>
        /// Creates mesh that occludes the sun bloom
        /// </summary>
        private MyEntity CreateOccluderEntity()
        {
            MyEntity ent = new MyEntity();
            ent.Init(null, ModContext.ModPath + @"\Models\Water2.mwm", null, 250, null);
            ent.Render.CastShadows = false;
            ent.Render.DrawOutsideViewDistance = true;
            ent.IsPreview = true;
            ent.Save = false;
            ent.SyncFlag = false;
            ent.NeedsWorldMatrix = false;
            ent.Flags |= EntityFlags.IsNotGamePrunningStructureObject;
            MyEntities.Add(ent, false);
            ent.Render.Transparency = 100f;
            ent.PositionComp.SetPosition(Vector3D.MaxValue);
            ent.Render.DrawInAllCascades = false;
            //ent.Render.UpdateTransparency();
            ent.InScene = true;
            ent.Render.UpdateRenderObject(true, false);

            return ent;
        }

        public void SimulatePlayer(Water water, IMyCharacter player)
        {
            Vector3D HeadPosition = player.GetHeadMatrix(false, false).Translation;
            Vector3D FootPosition = player.GetPosition();

            MyCubeGrid NearestGrid = WaterUtils.GetApproximateGrid(FootPosition);
            IMySlimBlock Nearestblock = NearestGrid?.GetTargetedBlock(HeadPosition);
            bool InHottub = false;
            bool InWater;
            float Depth = water.GetDepth(ref HeadPosition);

            if (Nearestblock != null && Nearestblock is IMyCollector)
                foreach (var Tub in FatBlockStorage.HotTubs)
                {
                    if (Tub.Block.Position == Nearestblock.Position)
                    {
                        float HeightOffset = -(Tub.Block.CubeGrid.GridSize / 2) + (((float)Tub.inventory.CurrentVolume / (float)Tub.inventory.MaxVolume) * (Tub.Block.CubeGrid.GridSize / 2)) + 0.1f;

                        if (Vector3D.Dot(Tub.Block.PositionComp.WorldMatrixRef.Up, Vector3D.Normalize(FootPosition - (Tub.Block.PositionComp.GetPosition() + (Tub.Block.PositionComp.WorldMatrixRef.Up * HeightOffset)))) < 0)
                            InHottub = true;

                        break;
                    }
                }

            InWater = InHottub || (Depth < 0 && (NearestGrid == null || !NearestGrid.IsRoomAtPositionAirtight(NearestGrid.WorldToGridInteger(HeadPosition))));

            if (InWater)
            {
                Vector3 GridVelocity = Vector3.Zero;
                if (NearestGrid?.RayCastBlocks(HeadPosition, HeadPosition + (Vector3.Normalize(NearestGrid.Physics.LinearVelocity) * 10)) != null)
                {
                    GridVelocity = (NearestGrid?.Physics?.LinearVelocity ?? Vector3D.Zero);
                }
                
                Vector3 Velocity = (-player.Physics.LinearVelocity + GridVelocity);
                Vector3 characterforce = (Velocity * Velocity.Length()) * 0.1f * player.Physics.Mass * (water.fluidDensity / 1000f);

                if ((!player.EnabledThrusts || !player.EnabledDamping) && WaterUtils.IsPlayerStateFloating(player.CurrentMovementState))
                    characterforce += player.Physics.Mass * -player.Physics.Gravity * 1.1f * Math.Min(water.buoyancy, 1f);

                if (characterforce.IsValid())
                    player.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, characterforce, null, null);

                MyCharacterOxygenComponent OxygenComponent;

                if (MyAPIGateway.Session.IsServer)
                {
                    //Drowning
                    if (MyAPIGateway.Session.SessionSettings.EnableOxygen && tickTimerPlayerDamage == 0 && (!player.Components.TryGet<MyCharacterOxygenComponent>(out OxygenComponent) || !OxygenComponent.HelmetEnabled))
                    {
                        if (player.ControllerInfo?.Controller?.ControlledEntity is IMyCharacter == true)
                            player.DoDamage(3f, MyDamageType.Asphyxia, true);
                    }

                    //Pressure Crushing
                    if (Depth <= -water.crushDepth)
                    {
                        if (player.ControllerInfo?.Controller?.ControlledEntity is IMyCharacter == true)
                            player.DoDamage(1f + 1f * (-(Depth + water.crushDepth) / 300), MyDamageType.Temperature, true);
                    }
                }
            }
        }

        public void SimulateGrid(Water water, MyCubeGrid grid)
        {
            if (grid == null || grid.Physics == null || grid.Physics.IsStatic)
                return;

            Vector3D gridCenter = grid.PositionComp.WorldVolume.Center;
            Vector3 linearVelocity = grid.Physics.LinearVelocity;
            Vector3 SqrlinearVelocity = linearVelocity * linearVelocity.Length();
            Vector3 angularVelocity = grid.Physics.AngularVelocity;
            Vector3 gravityDirection = Vector3.Normalize(grid.Physics.Gravity);
            Vector3D center = Vector3.Zero;
            //Vector3 dragCenter = Vector3.Zero;
            float dragArea = 0;
            float underwaterBlockCount = 0;
            int totalBlockcount = 0;
            double litres = 0;
            float capacity = 0;
            float depth = water.GetDepth(ref gridCenter);
            float totalTanks = 0;
            float WaterDensityMultiplier = water.fluidDensity / 1000f;

            ListReader<FatBlockStorage.BlockStorage> fatBlocks;
            if (FatBlockStorage.Storage.TryGetValue(grid.EntityId, out fatBlocks))
            {
                MyAPIGateway.Parallel.For(0, fatBlocks.Count, i =>
                {
                    totalBlockcount++;

                    BlockStorage blockStorage = fatBlocks[i];
                    MyCubeBlock block = blockStorage.Block;

                    Vector3D worldPosition = block.PositionComp.GetPosition();
                    Vector3I blockPosition = block.Position;
                    Vector3 blocKVelocity = grid.Physics.GetVelocityAtPoint(worldPosition);
                    float BlockDepth = water.GetDepth(ref worldPosition);
                    float FixedDepth = MathHelper.Min(-BlockDepth, 1);
                    bool Underwater = BlockDepth < 0;

                    if (grid.BlocksDestructionEnabled)
                        if (Underwater != blockStorage.PreviousUnderwaterServer)
                        {
                            blockStorage.PreviousUnderwaterServer = Underwater;

                            if (Underwater)
                            {
                                //Enters water
                                //Vector3 vertVelocity = grid.Physics.GetVelocityAtPoint(worldPosition) * gravityDirection;

                                Vector3 vertVelocity = Vector3.ProjectOnVector(ref blocKVelocity, ref gravityDirection);
                                float vertVelocityLength = vertVelocity.Length();
                                if (vertVelocity.IsValid() && vertVelocityLength > 30)
                                {
                                    DamageQueue.Push(new MyTuple<MyCubeBlock, float>(block, 25 * (vertVelocityLength / 10f) * WaterDensityMultiplier));
                                }
                            }
                        }

                    if (Underwater)
                    {
                        dragArea = Math.Max(Vector3.DistanceSquared(gridCenter, worldPosition), dragArea);

                        //dragCenter += blockPosition;
                        underwaterBlockCount += FixedDepth;

                        if (!block.IsFunctional)
                            return;

                        if (block is IMyGasTank)
                        {
                            totalTanks++;

                            IMyGasTank gasTank = block as IMyGasTank;
                            capacity = (float)gasTank.FilledRatio * gasTank.Capacity * FixedDepth;
                            litres += capacity;
                            center += block.Position * capacity;

                            return;
                        }

                        if (block is IMyWheel)
                        {
                            totalTanks++;

                            if (grid.GridSizeEnum == MyCubeSize.Large)
                                capacity = block.BlockDefinition.Mass * 31 * FixedDepth;
                            else
                                capacity = block.BlockDefinition.Mass * 25 * FixedDepth;

                            litres += capacity;
                            center += block.Position * capacity;

                            return;
                        }
                    }
                }, 1000);

                if (underwaterBlockCount != 0)
                {
                    float PercentUnderwater = (underwaterBlockCount / totalBlockcount);

                    if (litres != 0)
                    {
                        Vector3D centerOfBuoyancy = grid.GridIntegerToWorld(center / litres);

                        double force = 0;
                        if (grid.GridSizeEnum == MyCubeSize.Large)
                            force = litres * (1 + (-depth / 1000f)) / 50f * water.buoyancy * WaterDensityMultiplier;
                        else
                            force = litres * (1 + (-depth / 1000f)) / 20f * water.buoyancy * WaterDensityMultiplier;

                        //force -= Math.Abs(Vector3.Dot(grid.Physics.LinearVelocity, Vector3D.Normalize(grid.Physics.Gravity))) * (grid.Physics.Speed * grid.Physics.Mass) * 0.01;
                        force += Vector3.Dot(Vector3D.Normalize(linearVelocity), gravityDirection) * (linearVelocity.Length() * grid.Physics.Mass) * 0.05;
                        Vector3 newForce = -gravityDirection * (float)force * 40f;

                        if (newForce.IsValid())
                            grid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, newForce, centerOfBuoyancy, null, null, false, false);
                    }

                    //Drag
                    float physicsOptimizer = water.fluidDensity * PercentUnderwater * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS * (dragArea * 1.2f);

                    Vector3 AngularForce = (physicsOptimizer * (angularVelocity * angularVelocity.Length())) / grid.Physics.Mass;

                    if (AngularForce.IsValid())
                        grid.Physics.AngularVelocity -= AngularForce;

                    if (DragAPI.Heartbeat)
                    {
                        var api = dragAPIGetter.GetOrAdd(grid, DragClientAPI.DragObject.Factory);

                        if (api != null)
                        {
                            api.ViscosityMultiplier = 1f + (PercentUnderwater * water.fluidDensity);
                        }
                    }
                    else
                    {
                        Vector3 DragForce = physicsOptimizer * SqrlinearVelocity;

                        if (DragForce.IsValid())
                            grid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, -DragForce, null, null, null, false, false);
                        /*if (DragForce.IsValid())
                            grid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, -DragForce, grid.GridIntegerToWorld(Vector3.Lerp(dragCenter / underwaterBlockCount, grid.Physics.CenterOfMassLocal, PercentUnderwater)), null, null, false, false);*/
                    }
                }
            }

            underwaterBlockCount += grid.BlocksCount - totalBlockcount;
        }

        /// <summary>
        /// Draw
        /// </summary>
        public override void Draw()
        {
            Vector4 WhiteColor = new Vector4(nightValue, nightValue, nightValue, 1);

            if (MyAPIGateway.Session.Camera?.WorldMatrix != null)
            {
                Session.CameraPosition = MyAPIGateway.Session.Camera.Position;
                Session.CameraRotation = MyAPIGateway.Session.Camera.WorldMatrix.Forward;
                Session.CameraMatrix = MyAPIGateway.Session.Camera.WorldMatrix;
            }

            if (Session.GravityDirection != Vector3D.Zero)
            {
                Session.GravityAxisA = Vector3D.CalculatePerpendicularVector(Session.GravityDirection);
                Session.GravityAxisB = Session.GravityAxisA.Cross(Session.GravityDirection);
            }

            if (closestWater != null)
            {
                Session.DistanceToHorizon = Session.CameraUnderwater ? 500 : (float)Math.Sqrt(((Session.CameraDepth + closestWater.radius) * (Session.CameraDepth + closestWater.radius)) - (closestWater.radius * closestWater.radius));

                WaterData.UpdateFovFrustum();

                if (sunOccluder != null)
                {
                    sunOccluder.Render.Visible = Session.CameraUnderwater || (closestWater.Intersects(Session.CameraPosition, Session.CameraPosition + (Session.SunDirection * 100)) != 0);

                    if (sunOccluder.Render.Visible)
                        sunOccluder.WorldMatrix = MatrixD.CreateWorld(Session.CameraPosition + (Session.SunDirection * 1000), -Vector3.CalculatePerpendicularVector(Session.SunDirection), -Session.SunDirection);
                }

                if (Session.CameraUnderwater && particleOccluder != null)
                    particleOccluder.WorldMatrix = MatrixD.CreateWorld(Session.CameraPosition + (Session.CameraRotation * 250), -Vector3.CalculatePerpendicularVector(Session.CameraRotation), -Session.CameraRotation);
                else
                    particleOccluder.WorldMatrix = MatrixD.CreateWorld(closestWater.GetClosestSurfacePoint(ref Session.CameraPosition) + (Session.GravityDirection * 20), Session.GravityAxisA, -Session.GravityDirection);

                Session.CameraDepth = closestWater.GetDepth(ref Session.CameraPosition);
                Session.CameraHottub = WaterUtils.HotTubUnderwater(Session.CameraPosition);
                Session.CameraUnderwater = Session.CameraDepth <= 0 || Session.CameraHottub;

                if (Session.CameraHottub)
                    Session.CameraAirtight = false;
            }
            else
            {
                Session.DistanceToHorizon = float.PositiveInfinity;
                Session.CameraDepth = 0;
                Session.CameraUnderwater = false;
            }

            if (Session.CameraUnderwater)
            {
                if (Settings.ShowFog || !MyAPIGateway.Session.CreativeMode)
                    weatherEffects.FogDensityOverride = (0.03f + (0.00125f * (1f + (-Session.CameraDepth / 100f)))) * ((25f - Math.Max(Session.InsideGrid - 10, 0)) / 25f);
                else
                    weatherEffects.FogDensityOverride = 0.001f;

                weatherEffects.FogMultiplierOverride = 1;

                weatherEffects.FogColorOverride = closestWater.lit ? Vector3D.Lerp(closestWater.fogColor, Vector3D.Zero, -Session.CameraDepth / closestWater.crushDepth) : closestWater.fogColor;
                weatherEffects.FogAtmoOverride = 1;
                weatherEffects.FogSkyboxOverride = 1;
                //weatherEffects.SunColorOverride = closestWater.fogColor;
                //weatherEffects.SunSpecularColorOverride = closestWater.fogColor;
                weatherEffects.SunIntensityOverride = Math.Max(5 - (-Math.Min(Session.CameraDepth, -10) / 15f), 0.0001f);
            }

            //Only Change fog once above water and all the time underwater
            if (previousUnderwaterState != Session.CameraUnderwater)
            {
                previousUnderwaterState = Session.CameraUnderwater;

                if (Session.CameraUnderwater)
                {
                    //weatherEffects.FogColorOverride = Vector3D.Lerp(new Vector3D(0.1, 0.125, 0.176), Vector3D.Zero, -depth / 1000.0);
                    weatherEffects.ParticleVelocityOverride = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                }
                else
                {
                    weatherEffects.FogDensityOverride = null;
                    weatherEffects.FogMultiplierOverride = null;
                    weatherEffects.FogColorOverride = null;
                    weatherEffects.FogSkyboxOverride = null;
                    weatherEffects.FogAtmoOverride = null;
                    weatherEffects.SunIntensityOverride = null;
                    weatherEffects.ParticleVelocityOverride = null;
                    //weatherEffects.SunColorOverride = null;
                    //weatherEffects.SunSpecularColorOverride = null;
                }
            }

            lock (effectLock)
            {
                foreach (var bubble in Bubbles)
                {
                    if (bubble == null)
                        continue;

                    MyTransparentGeometry.AddPointBillboard(WaterData.BubblesMaterial, WhiteColor * WaterData.BubbleColor * (1f - ((float)bubble.life / (float)bubble.maxLife)), bubble.position, bubble.radius, bubble.angle);
                }

                if (!Session.CameraUnderwater)
                    foreach (var splash in SimulatedSplashes)
                    {
                        if (splash == null)
                            continue;

                        MyTransparentGeometry.AddPointBillboard(WaterData.PhysicalSplashMaterials[WaterData.GetPhysicalSplashMaterial(splash.radius)], WhiteColor * WaterData.WhiteColor * (1f - ((float)splash.life / (float)splash.maxLife)), splash.position, splash.radius, splash.angle, blendType: BlendTypeEnum.Standard);
                    }

                if (Settings.ShowHud)
                    foreach (var indicator in indicators)
                    {
                        if (indicator == null)
                            continue;

                        if (indicator.gridSize == MyCubeSize.Small)
                            MyTransparentGeometry.AddPointBillboard(WaterData.IconMaterial, WaterData.WhiteColor, indicator.position, 0.25f, 0, blendType: BlendTypeEnum.AdditiveTop);
                        else
                            MyTransparentGeometry.AddPointBillboard(WaterData.IconMaterial, WaterData.WhiteColor, indicator.position, 0.75f, 0, blendType: BlendTypeEnum.AdditiveTop);
                    }

                if (Session.CameraUnderwater && !Session.CameraAirtight)
                {
                    //tint
                    //MyTransparentGeometry.AddBillboardOriented(WaterData.BlankMaterial, new Vector4(0.255f, 0.501f, 0.913f, 0.1f), Session.CameraPosition + Session.CameraRotation, MyAPIGateway.Session.Camera.WorldMatrix.Left, MyAPIGateway.Session.Camera.WorldMatrix.Up, 10000, blendType: BlendTypeEnum.AdditiveTop);

                    foreach (var bubble in SmallBubbles)
                    {
                        if (bubble == null)
                            continue;

                        MyTransparentGeometry.AddPointBillboard(WaterData.FireflyMaterial, bubble.color * (1f - ((float)bubble.life / bubble.maxLife)), bubble.position, bubble.radius, bubble.angle);
                    }

                    if (closestWater?.enableFish == true)
                    {
                        foreach (var fish in Fishes)
                        {
                            if (fish == null)
                                continue;

                            MyTransparentGeometry.AddBillboardOriented(WaterData.FishMaterials[fish.textureId], WaterData.WhiteColor, fish.Position, fish.LeftVector, fish.UpVector, 0.7f);
                        }
                    }
                }
                else
                {
                    if (closestWeather == null && closestWater?.enableSeagulls == true && nightValue > 0.75f && closestWeather == null)
                        foreach (var seagull in Seagulls)
                        {
                            if (seagull == null)
                                continue;

                            //MyTransparentGeometry.AddPointBillboard(seagullMaterial, WaterData.WhiteColor, seagull.position, 1, 0);
                            MyTransparentGeometry.AddBillboardOriented(WaterData.SeagullMaterial, WhiteColor, seagull.Position, seagull.LeftVector, seagull.UpVector, 0.7f);
                        }
                }
            }

            foreach (var Billboard in QuadBillboards)
            {
                MyQuadD Quad = Billboard.Quad;
                //MyTransparentGeometry.AddQuad(Billboard.Material, ref Quad,  Vector4.One, ref zeroVector);
                MyTransparentGeometry.AddQuad(Billboard.Material, ref Quad, Billboard.Color, ref zeroVector);
            }

            if (Session.CameraDepth > -200)
            {
                if (Vector3D.RectangularDistance(Session.LastLODBuildPosition, Session.CameraPosition) > 15)
                {
                    RebuildLOD();
                    DrawAfterSimulation();
                }
            }
        }

        /// <summary>
        /// Recreates water to use any changed settings
        /// </summary>
        public void RecreateWater()
        {
            if ((int)Math.Min(160 * Settings.Quality, 128) != SmallBubbles.Length)
                SmallBubbles = new SmallBubble[(int)Math.Min(160 * Settings.Quality, 128)];

            if ((int)Math.Min(256 * Settings.Quality, 128) != Seagulls.Length)
            {
                foreach (var seagull in Seagulls)
                {
                    if (seagull?.cawSound?.IsPlaying == true)
                        seagull.cawSound.StopSound(true, true);
                }

                Seagulls = new Seagull[(int)Math.Min(256 * Settings.Quality, 128)];
            }

            if ((int)Math.Min(128 * Settings.Quality, 64) != Fishes.Length)
            {
                Fishes = new Fish[(int)Math.Min(128 * Settings.Quality, 64)];
            }

            /*if ((int)Math.Min(96 * Settings.Quality, 128) != GodRays.Length)
            {
                GodRays = new GodRay[(int)Math.Min(96 * Settings.Quality, 128)];
            }*/

            if (Waters != null)
                foreach (var water in Waters.Values)
                {
                    water.UpdateTexture();
                }
        }

        /// <summary>
        /// Simulates sounds like cawing and ambience
        /// </summary>
        public void SimulateSounds()
        {
            if (closestPlanet?.HasAtmosphere == true && closestWater != null)
            {
                //Music
                if (Session.CameraAboveWater && Session.CameraDepth < 250)
                {
                    //Above Water
                    MyVisualScriptLogicProvider.MusicSetDynamicMusic(false);

                    if (musicCategory != "Calm")
                    {
                        MyVisualScriptLogicProvider.MusicPlayMusicCategory("Calm");

                        musicCategory = "Calm";
                    }
                }
                else if (Session.CameraUnderwater)
                {
                    if (Session.CameraDepth < -closestWater.crushDepth)
                    {
                        //In crush depth
                        MyVisualScriptLogicProvider.MusicSetDynamicMusic(false);

                        if (musicCategory != "Mystery")
                        {
                            MyVisualScriptLogicProvider.MusicPlayMusicCategory("Mystery");

                            musicCategory = "Mystery";
                        }
                    }
                    else
                    {
                        //Just Underwater
                        MyVisualScriptLogicProvider.MusicSetDynamicMusic(false);

                        if (musicCategory != "Space")
                        {
                            MyVisualScriptLogicProvider.MusicPlayMusicCategory("Space");

                            musicCategory = "Space";
                        }
                    }
                }
                else
                {
                    //Reset
                    MyVisualScriptLogicProvider.MusicSetDynamicMusicLocal(true);
                    musicCategory = "";
                }

                //MyAPIGateway.Utilities.ShowNotification("" + musicCategory, 16);

                if (Session.CameraPosition != null)
                {
                    if (AmbientBoatSoundEmitter == null || AmbientSoundEmitter == null || EnvironmentOceanSoundEmitter == null || EnvironmentBeachSoundEmitter == null || EnvironmentUnderwaterSoundEmitter == null)
                        return;

                    if (Session.CameraUnderwater)
                    {
                        ambientTimer--;
                        if (ambientTimer <= 0)
                        {
                            ambientTimer = MyUtils.GetRandomInt(1000, 1900); //Divide by 60 to get in seconds

                            if (!AmbientSoundEmitter.IsPlaying)
                            {
                                AmbientSoundEmitter.PlaySound(WaterData.AmbientSound);
                                AmbientSoundEmitter.SetPosition(Session.CameraPosition + (MyUtils.GetRandomVector3Normalized() * MyUtils.GetRandomFloat(0, 75)));
                                AmbientSoundEmitter.VolumeMultiplier = Settings.Volume;
                            }
                        }

                        if (!EnvironmentUnderwaterSoundEmitter.IsPlaying)
                            EnvironmentUnderwaterSoundEmitter.PlaySound(WaterData.EnvironmentUnderwaterSound, force2D: true);

                        if (!EnvironmentUndergroundSoundEmitter.IsPlaying)
                            EnvironmentUndergroundSoundEmitter.StopSound(true, false);

                        if (EnvironmentBeachSoundEmitter.IsPlaying)
                            EnvironmentBeachSoundEmitter.StopSound(true, false);

                        if (EnvironmentOceanSoundEmitter.IsPlaying)
                            EnvironmentOceanSoundEmitter.StopSound(true, false);

                        ambientBoatTimer--;

                        if (ambientBoatTimer <= 0)
                        {
                            ambientBoatTimer = MyUtils.GetRandomInt(1000, 1500); //Divide by 60 to get in seconds

                            if (Session.CameraUnderwater && !AmbientBoatSoundEmitter.IsPlaying && Session.InsideGrid > 10 && Session.CameraDepth < -closestWater.crushDepth)
                            {
                                AmbientBoatSoundEmitter.PlaySound(WaterData.GroanSound);
                                AmbientBoatSoundEmitter.VolumeMultiplier = Settings.Volume;
                                AmbientBoatSoundEmitter.SetPosition(Session.CameraPosition + (MyUtils.GetRandomVector3Normalized() * MyUtils.GetRandomFloat(0, 75)));
                            }
                        }
                    }
                    else
                    {

                        if (!EnvironmentOceanSoundEmitter.IsPlaying)
                            EnvironmentOceanSoundEmitter.PlaySound(WaterData.EnvironmentOceanSound);

                        if (!EnvironmentUndergroundSoundEmitter.IsPlaying)
                            EnvironmentUndergroundSoundEmitter.PlaySound(WaterData.EnvironmentUndergroundSound, force2D: true);

                        //EnvironmentBeachSoundEmitter.SetPosition(closestPlanet.GetClosestSurfacePointGlobal(cameraPosition));

                        if (!EnvironmentBeachSoundEmitter.IsPlaying)
                            EnvironmentBeachSoundEmitter.PlaySound(WaterData.EnvironmentBeachSound, force2D: true);

                        if (closestWeather == null && Seagulls != null && closestWater.enableSeagulls && nightValue > 0.75f && closestWeather == null)
                            foreach (var seagull in Seagulls)
                            {
                                if (seagull == null)
                                    continue;

                                if (MyUtils.GetRandomInt(0, 150) < 1)
                                {
                                    seagull.Caw();
                                }
                            }

                        if (EnvironmentUnderwaterSoundEmitter.IsPlaying)
                            EnvironmentUnderwaterSoundEmitter.StopSound(true, false);
                    }

                    float volumeMultiplier = (25f - Math.Max(Math.Min(Session.InsideGrid + Session.InsideVoxel, 25) - 5, 0)) / 25f * Settings.Volume;
                    EnvironmentUnderwaterSoundEmitter.VolumeMultiplier = volumeMultiplier;
                    EnvironmentOceanSoundEmitter.VolumeMultiplier = volumeMultiplier * MyMath.Clamp((100f - Session.CameraDepth) / 100f, 0, 1f) * ((25f - Session.InsideVoxel) / 25f);
                    EnvironmentBeachSoundEmitter.VolumeMultiplier = volumeMultiplier * MyMath.Clamp((50f - Session.CameraDepth) / 50f, 0, 1f) * ((25f - Session.InsideVoxel) / 25f);
                    EnvironmentUndergroundSoundEmitter.VolumeMultiplier = volumeMultiplier * (Session.InsideVoxel / 25f) * (Session.CameraAboveWater ? 0 : 1);
                }
            }
            else
            {
                if (EnvironmentBeachSoundEmitter.IsPlaying)
                    EnvironmentBeachSoundEmitter.StopSound(true);

                if (EnvironmentOceanSoundEmitter.IsPlaying)
                    EnvironmentOceanSoundEmitter.StopSound(true);

                if (EnvironmentUnderwaterSoundEmitter.IsPlaying)
                    EnvironmentUnderwaterSoundEmitter.StopSound(true);

                if (EnvironmentUndergroundSoundEmitter.IsPlaying)
                    EnvironmentUndergroundSoundEmitter.StopSound(true);
            }
        }

        /// <summary>
        /// Simulates bubbles, splashes, wakes, indicators, smallBubbles, seagulls, and fish
        /// </summary>
        public void SimulateEffects()
        {
            float bubbleSpeed = closestPlanet != null ? ((MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS / 2) * closestPlanet.Generator.SurfaceGravity) : ((MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS / 2));

            lock (effectLock)
            {
                if (Bubbles != null && Bubbles.Count > 0)
                    for (int i = Bubbles.Count - 1; i >= 0; i--)
                    {
                        Bubble bubble = Bubbles[i];
                        if (bubble == null || bubble.life > bubble.maxLife || closestPlanet == null)
                        {
                            Bubbles.RemoveAtFast(i);
                            continue;
                        }
                        bubble.position += -Session.GravityDirection * bubbleSpeed;
                        bubble.life++;
                    }
                if (Splashes != null && Splashes.Count > 0)
                    for (int i = Splashes.Count - 1; i >= 0; i--)
                    {
                        Splash splash = Splashes[i];
                        if (splash == null || splash.life > splash.maxLife || closestPlanet == null)
                        {
                            Splashes.RemoveAtFast(i);
                            continue;
                        }
                        splash.life++;
                    }

                if (SimulatedSplashes != null && SimulatedSplashes.Count > 0)
                    for (int i = SimulatedSplashes.Count - 1; i >= 0; i--)
                    {
                        SimulatedSplash simulatedSplash = SimulatedSplashes[i];
                        if (simulatedSplash == null || simulatedSplash.life > simulatedSplash.maxLife || closestPlanet == null)
                        {
                            SimulatedSplashes.RemoveAtFast(i);
                            continue;
                        }
                        simulatedSplash.life++;
                        simulatedSplash.velocity += Session.Gravity * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS;
                        simulatedSplash.position += simulatedSplash.velocity;
                        simulatedSplash.angle += MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS;

                        if (!simulatedSplash.under && closestWater.IsUnderwater(ref simulatedSplash.position))
                        {
                            simulatedSplash.under = true;
                            Splashes.Add(new Splash(simulatedSplash.position, simulatedSplash.radius * 2));
                        }

                        if (simulatedSplash.under)
                        {
                            simulatedSplash.life += 10;
                        }
                    }

                /*if (Wakes != null && Wakes.Count > 0)
                    for (int i = Wakes.Count - 1; i >= 0; i--)
                    {
                        if (Wakes[i] == null || Wakes[i].life > Wakes[i].maxLife || closestPlanet == null)
                        {
                            Wakes.RemoveAtFast(i);
                            continue;
                        }
                        Wakes[i].position += Wakes[i].velocity;
                        Wakes[i].life++;
                        Wakes[i].velocity *= 0.99f;
                    }*/

                if (indicators != null && indicators.Count > 0)
                    for (int i = indicators.Count - 1; i >= 0; i--)
                    {
                        COBIndicator indicator = indicators[i];
                        if (indicator == null || indicator.life > tickTimerMax + 1 || closestPlanet == null)
                        {
                            indicators.RemoveAtFast(i);
                            continue;
                        }
                        indicator.life++;
                    }

                if (SmallBubbles != null && Session.CameraUnderwater)
                    for (int i = SmallBubbles.Length - 1; i >= 0; i--)
                    {
                        SmallBubble bubble = SmallBubbles[i];
                        if (bubble == null || bubble.life > bubble.maxLife)
                        {
                            SmallBubbles[i] = new SmallBubble(Session.CameraPosition + (MyUtils.GetRandomVector3Normalized() * MyUtils.GetRandomFloat(5, 20)));
                            continue;
                        }
                        bubble.position += -Session.GravityDirection * bubbleSpeed;
                        bubble.life++;
                    }

                if (Seagulls != null && closestWater?.enableSeagulls == true)
                    for (int i = 0; i < Seagulls.Length; i++)
                    {
                        Seagull seagull = Seagulls[i];
                        if (seagull == null || seagull.Life > seagull.MaxLife)
                        {
                            if (seagull?.cawSound?.IsPlaying == true)
                                continue;

                            Seagulls[i] = new Seagull(Session.CameraClosestPosition + (-Session.GravityDirection * MyUtils.GetRandomFloat(25, 150)) + (MyUtils.GetRandomPerpendicularVector(Session.GravityDirection) * MyUtils.GetRandomFloat(-2500, 2500)), MyUtils.GetRandomPerpendicularVector(Session.GravityDirection) * MyUtils.GetRandomFloat(0.05f, 0.15f), -Session.GravityDirection);
                            continue;
                        }

                        seagull.Life++;
                        seagull.Position += seagull.Velocity;
                    }

                if (Fishes != null && closestWater?.enableFish == true)
                    for (int i = 0; i < Fishes.Length; i++)
                    {
                        Fish fish = Fishes[i];
                        if (fish == null || fish.Life > fish.MaxLife)
                        {
                            Vector3D Position = Session.CameraPosition + (MyUtils.GetRandomVector3Normalized() * MyUtils.GetRandomFloat(50, 250));
                            Vector3D Direction = Vector3D.Zero;
                            for (int j = 0; j < 5; j++)
                            {
                                Direction = MyUtils.GetRandomPerpendicularVector(Session.GravityDirection);

                                if (!WaterUtils.IsUnderGround(closestPlanet, Position) && !WaterUtils.IsUnderGround(closestPlanet, Position + Direction))
                                    break;
                            }

                            Fishes[i] = new Fish(Position, Direction * MyUtils.GetRandomFloat(0.05f, 0.11f), Session.GravityDirection);
                            continue;
                        }

                        fish.Life++;
                        fish.Position += fish.Velocity;
                    }

                /*if (GodRays != null)
                    for (int i = 0; i < GodRays.Length; i++)
                    {
                        if (GodRays[i] == null || GodRays[i].Life > GodRays[i].MaxLife)
                        {
                            GodRays[i] = new GodRay(closestWater.GetClosestSurfacePoint(Session.CameraPosition + (MyUtils.GetRandomVector3Normalized() * MyUtils.GetRandomFloat(1, 250))), 1);
                        }

                        GodRays[i].Life++;
                    }*/

                /*foreach (var wake in Wakes.ToArray())
                {
                    Wakes[wake.Key]--;

                    if (Wakes[wake.Key] < 0)
                        Wakes.Remove(wake.Key);
                }*/
            }
        }

        /// <summary>
        /// Player connects to server event
        /// </summary>
        private void PlayerConnected(long playerId)
        {
            players.Clear();
            MyAPIGateway.Players.GetPlayers(players);

            if (MyAPIGateway.Session.IsServer)
                SyncClients();
        }

        /// <summary>
        /// Player disconnected from server event
        /// </summary>
        private void PlayerDisconnected(long playerId)
        {
            players.Clear();
            MyAPIGateway.Players.GetPlayers(players);
        }

        /// <summary>
        /// Syncs data between clients
        /// </summary>
        public void SyncClients()
        {
            if (MyAPIGateway.Session.IsServer && Waters != null)
            {
                MyAPIGateway.Multiplayer.SendMessageToOthers(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(Waters)));
            }
        }

        /// <summary>
        /// Client -> Server and Server -> Client communication
        /// </summary>
        /// <param name="packet"></param>
        private void ClientHandler(byte[] packet)
        {
            Dictionary<long, Water> TempWaters = MyAPIGateway.Utilities.SerializeFromBinary<SerializableDictionary<long, Water>>(packet).Dictionary;

            if (TempWaters != null)
            {
                foreach (var TempWater in TempWaters)
                {
                    TempWater.Value.Init();
                }

                Waters = TempWaters;
            }

            //if (Waters != null)
            //MyAPIGateway.Utilities.SendModMessage(WaterModAPI.ModHandlerID, MyAPIGateway.Utilities.SerializeToBinary(Waters));

            if (!MyAPIGateway.Utilities.IsDedicated)
            {
                RecreateWater();
                RebuildLOD();
            }
            if (MyAPIGateway.Session.IsServer)
                SyncClients();
        }
    }

    public class InvalidModException : Exception
    {
        public InvalidModException() : base("Invalid Water Mod ID detected, ensure you're using the official version!")
        {

        }
    }
}