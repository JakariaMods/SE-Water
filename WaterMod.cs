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
using ProtoBuf;
using VRageRender;
using Jakaria.Configs;
using Jakaria.Components;

namespace Jakaria
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    class WaterMod : MySessionComponentBase
    {
        List<IMyPlayer> players = new List<IMyPlayer>();

        //Water Effects
        //public List<Water> waters = new List<Water>();
        public Dictionary<long, Water> Waters = new Dictionary<long, Water>();
        //public Dictionary<long, MyTuple<float, float>> WheelStorage = new Dictionary<long, MyTuple<float, float>>();

        //public Dictionary<Vector3I, float> Wakes = new Dictionary<Vector3I, float>();
        public List<Splash> SurfaceSplashes = new List<Splash>(128);
        Seagull[] Seagulls = new Seagull[16];
        public Fish[] Fishes = new Fish[32];

        List<AnimatedPointBillboard> Bubbles = new List<AnimatedPointBillboard>();
        AnimatedPointBillboard[] AmbientBubbles = new AnimatedPointBillboard[256];

        public ConcurrentStack<QuadBillboard> QuadBillboards = new ConcurrentStack<QuadBillboard>();
        public List<MyBillboard> BillboardCache = new List<MyBillboard>();
        public List<SimulatedSplash> SimulatedSplashes = new List<SimulatedSplash>();
        public ConcurrentStack<MyTuple<IMySlimBlock, float>> DamageQueue = new ConcurrentStack<MyTuple<IMySlimBlock, float>>();

        public Action UpdateAfter1;
        public Action UpdateAfter60;

        FastNoiseLite GeneralNoise = new FastNoiseLite();
        double GeneralNoiseTimer = 0;
        Vector3D zeroVector = Vector3D.Zero;

        //Draygo
        public ConcurrentDictionary<IMyEntity, DragClientAPI.DragObject> dragAPIGetter = new ConcurrentDictionary<IMyEntity, DragClientAPI.DragObject>();
        public DragClientAPI DragAPI = new DragClientAPI();
        public WcApi WeaponCoreAPI = new WcApi();

        public Object effectLock = new Object();

        [ProtoContract]
        public static class Settings
        {
            [ProtoMember(1)]
            public static float Quality = 1.5f;

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

        /// <summary>
        /// Clientside variables
        /// </summary>
        public static class Session
        {
            public static bool CameraAboveWater = false;
            public static Vector3D CameraClosestPosition = Vector3D.Zero;
            public static bool CameraUnderground = false;
            public static double CameraDepth = 0;
            public static bool CameraAirtight = false;
            public static bool CameraUnderwater = false;
            public static bool CameraHottub = false;
            public static int InsideGrid = 0;
            public static int InsideVoxel = 0;
            public static Vector3D CameraPosition = Vector3.Zero;
            public static Vector3D CameraRotation = Vector3.Zero;
            public static Vector3 SunDirection = Vector3.Zero;
            public static float SessionTimer = 0;
            /// <summary>
            /// Direction vector that points DOWN towards surface
            /// </summary>
            public static Vector3D GravityDirection = Vector3.Zero;
            public static Vector3D CameraClosestWaterPosition = Vector3D.Zero;
            public static Vector3D Gravity = Vector3.Zero;
            public static Vector3D GravityAxisA = Vector3D.Zero;
            public static Vector3D GravityAxisB = Vector3D.Zero;
            public static Vector3D LastLODBuildPosition = Vector3D.MaxValue;
            public static float DistanceToHorizon = 0;
            public static double CameraAltitude = 0;
            public static MyPlanet ClosestPlanet = null;
            public static Water ClosestWater = null;
        }

        public ConcurrentCachingList<WaterCollectorComponent> HotTubs = new ConcurrentCachingList<WaterCollectorComponent>();
        MyObjectBuilder_WeatherEffect closestWeather = null;
        bool previousUnderwaterState = true;
        bool initialized = false;
        float nightValue = 0;
        IMyWeatherEffects weatherEffects;
        MyEntity sunOccluder;
        MyEntity particleOccluder;

        private WaterUIComponent uiComponent;

        //Timers
        int tickTimer = 0;
        public const int tickTimerMax = 5;
        int tickTimerSecond = 0;
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

        public override void BeforeStart()
        {
            WaterModAPIBackend.BeforeStart();
        }

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.WaterModVersion.Replace("{0}", WaterData.Version + (WaterData.EarlyAccess ? "EA" : "")));

            bool valid = false;

            foreach (var mod in MyAPIGateway.Session.Mods)
            {
                //Water Mod 2200451495
                //Water Mod Dev 2379346707

                if (mod.PublishedFileId == 0 || mod.PublishedFileId == 2200451495 || mod.PublishedFileId == 2379346707 || mod.PublishedFileId == 2713901399)
                    valid = true;

                if (mod.PublishedFileId == 2379346707)
                {
                    if (ModContext.ModItem.PublishedFileId == 2200451495) //Prevent duplicates
                    {
                        valid = false;
                        break;
                    }
                }
            }

            if (!valid)
            {
                UpdateOrder = MyUpdateOrder.NoUpdate;
                return;
            }

            MyAPIGateway.Multiplayer.RegisterMessageHandler(WaterData.ClientHandlerID, ClientHandler);

            MyLog.Default.WriteLine(WaterLocalization.CurrentLanguage.WaterModVersion.Replace("{0}", WaterData.Version + (WaterData.EarlyAccess ? "EA" : "")));

            weatherEffects = MyAPIGateway.Session.WeatherEffects;

            MyEntities.OnEntityCreate += Entities_OnEntityCreate;

            //Client
            if (!MyAPIGateway.Utilities.IsDedicated)
            {
                sunOccluder = CreateOccluderEntity();
                particleOccluder = CreateOccluderEntity();

                MyAPIGateway.Utilities.MessageEntered += Utilities_MessageEntered;
                MyExplosions.OnExplosion += MyExplosions_OnExplosion;

                RecreateWater();
            }

            //Server
            if (MyAPIGateway.Session.IsServer)
            {
                MyVisualScriptLogicProvider.RespawnShipSpawned += RespawnShipSpawned;
                MyEntities.OnEntityAdd += Entities_OnEntityAdd;
                MyVisualScriptLogicProvider.PlayerConnected += PlayerConnected;
                MyVisualScriptLogicProvider.PlayerDisconnected += PlayerDisconnected;
            }

            try
            {
                TextReader reader = MyAPIGateway.Utilities.FileExistsInGlobalStorage("WaterPSACounter")
                    ? MyAPIGateway.Utilities.ReadFileInGlobalStorage("WaterPSACounter")
                    : null;
                int psaCounter = reader != null ? int.Parse(reader.ReadLine()) : 0;
                DateTime lastPSA = reader != null ? DateTime.Parse(reader.ReadLine()) : WaterData.PSADate;

                if (lastPSA != WaterData.PSADate)
                    psaCounter = 0;

                if (psaCounter < WaterData.PSAFrequency)
                {
                    reader?.Close();
                    psaCounter++;

                    TextWriter writer = MyAPIGateway.Utilities.WriteFileInGlobalStorage("WaterPSACounter");
                    writer.WriteLine(psaCounter);
                    writer.WriteLine(lastPSA.ToString());
                    writer.Close();
                    int daysUntilPSA = (WaterData.PSADate - DateTime.Today).Days;
                    if (daysUntilPSA > 0)
                    {
                        MyAPIGateway.Utilities.ShowMissionScreen("Water Mod 3.0 PSA", "",
                            WaterData.PSACountdownText.Replace("{PSATimes}",
                                (WaterData.PSAFrequency - psaCounter).ToString()),
                            WaterData.PSAText.Replace("{PSADateCountdown}", daysUntilPSA.ToString())
                                .Replace("{PSADate}", WaterData.PSADate.ToLongDateString()));
                    }
                }
            }
            catch(Exception e)
            {
                WaterUtils.ShowMessage(e.ToString());
                WaterUtils.WriteLog(e.ToString());
            }

            if(!MyAPIGateway.Session.SessionSettings.EnableOxygen || !MyAPIGateway.Session.SessionSettings.EnableOxygenPressurization)
            {
                WaterUtils.ShowMessage("Oxygen/Airtightness is disabled, Airtightness flotation will not work.");
            }

            uiComponent = WaterUIComponent.Static;

            initialized = true;
        }

        private void MyExplosions_OnExplosion(ref MyExplosionInfo explosionInfo)
        {
            if (!MyAPIGateway.Utilities.IsDedicated && WaterMod.Session.ClosestWater != null && explosionInfo.ExplosionType != MyExplosionTypeEnum.GRID_DEFORMATION && explosionInfo.ExplosionType != MyExplosionTypeEnum.GRID_DESTRUCTION)
            {
                float ExplosionRadius = (float)explosionInfo.ExplosionSphere.Radius;
                Vector3D ExplosionPosition = explosionInfo.ExplosionSphere.Center;

                double Depth = WaterMod.Session.ClosestWater.GetDepth(ref ExplosionPosition);

                if (Depth - ExplosionRadius <= 0)
                {
                    float SurfaceRatio = Math.Max(1f - (Math.Abs((float)Depth) / ExplosionRadius), 0f);

                    explosionInfo.Direction = -Session.GravityDirection;
                    explosionInfo.PlaySound = false;

                    if (Depth <= 0 && Session.CameraUnderwater)
                    {
                        explosionInfo.CreateParticleEffect = false;

                        if (explosionInfo.ExplosionType == MyExplosionTypeEnum.CUSTOM)
                        {
                            if (explosionInfo.CustomSound != null)
                            {
                                string SoundName = explosionInfo.CustomSound.ToString();
                                if (SoundName == "RealPoofExplosionCat1" || SoundName == "ArcPoofExplosionCat1")
                                    explosionInfo.CustomSound = WaterData.UnderwaterPoofSound;
                            }
                        }
                        else
                        {
                            explosionInfo.CustomSound = WaterData.UnderwaterExplosionSound;
                            explosionInfo.ExplosionType = MyExplosionTypeEnum.CUSTOM;
                            explosionInfo.PlaySound = true;
                        }

                        if (Depth + ExplosionRadius <= 0 && WaterMod.Session.ClosestWater.Material.DrawBubbles)
                            CreateBubble(ref ExplosionPosition, ExplosionRadius / 4 * SurfaceRatio);
                    }
                    else
                    {
                        explosionInfo.ExplosionType = MyExplosionTypeEnum.CUSTOM;
                        explosionInfo.CustomSound = WaterData.SurfaceExplosionSound;
                        explosionInfo.PlaySound = true;
                    }

                    Vector3D surfacePoint = WaterMod.Session.ClosestWater.GetClosestSurfacePoint(explosionInfo.ExplosionSphere.Center);


                    //CreatePhysicsSplash(surfacePoint, -Session.GravityDirection * 120 * SurfaceRatio, Math.Max(ExplosionRadius / 4, 2f), SurfaceRatio, (int)(explosionInfo.ExplosionSphere.Radius * 2 * SurfaceRatio));
                    CreateSplash(surfacePoint, ExplosionRadius * SurfaceRatio, true);
                }
            }

            return;
        }

        /// <summary>
        /// Returns closest water to a position
        /// </summary>
        public Water GetClosestWater(Vector3D position)
        {
            double distance = float.PositiveInfinity;
            Water closestWater = null;
            foreach (var water in Waters.Values)
            {
                double distance2 = Vector3D.DistanceSquared(position, water.Position);

                if (distance2 < distance)
                {
                    closestWater = water;

                    distance = distance2;
                }
            }

            return closestWater;
        }

        private void Entities_OnEntityAdd(MyEntity obj)
        {
            if (obj != null)
            {
                if (MyAPIGateway.Session.IsServer)
                {
                    if (obj is MyPlanet)
                    {
                        MyPlanet planet = obj as MyPlanet;

                        if (WaterUtils.HasWater(planet))
                            return;

                        PlanetConfig config;

                        if (WaterData.PlanetConfigs.TryGetValue(planet.Generator.Id, out config))
                        {
                            if (config.WaterSettings != null)
                                Waters[planet.EntityId] = new Water(planet, config.WaterSettings);
                        }
                        else
                        {
                            //Obsolete but has to stay :(
                            foreach (var weather in planet.Generator.WeatherGenerators)
                            {
                                if (weather.Voxel == "WATERMODDATA")
                                {
                                    WaterSettings settings;
                                    settings = MyAPIGateway.Utilities.SerializeFromXML<WaterSettings>(weather.Weathers?[0]?.Name);

                                    Waters[planet.EntityId] = new Water(planet, settings);
                                    SyncClients();
                                }
                            }
                        }

                        return;
                    }
                }
            }
        }

        #region Commands

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
                case "wdiscord":
                    sendToOthers = false;

                    MyVisualScriptLogicProvider.OpenSteamOverlayLocal(@"https://steamcommunity.com/linkfilter/?url=https://discord.gg/GrPK8cB");
                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.OpenDiscord);
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
                            quality = MathHelper.Clamp(quality, 0.4f, 3f);
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
                    if (uiComponent.Heartbeat)
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
                    if (uiComponent.Heartbeat)
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

                    if (MyAPIGateway.Session.PromoteLevel < MyPromoteLevel.SpaceMaster)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GenericNoPermissions);
                        return;
                    }

                    Settings.ShowFog = !Settings.ShowFog;
                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.ToggleFog);

                    break;

                //Toggle birds on a planet
                case "wbird":
                    sendToOthers = false;

                    if (MyAPIGateway.Session.PromoteLevel < MyPromoteLevel.Moderator)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GenericNoPermissions);
                        return;
                    }

                    if (WaterMod.Session.ClosestPlanet != null)
                    {
                        foreach (var water in Waters.Values)
                        {
                            if (water.PlanetID == WaterMod.Session.ClosestPlanet.EntityId)
                            {
                                water.EnableSeagulls = !water.EnableSeagulls;
                                SyncToServer(true);
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

                    if (WaterMod.Session.ClosestPlanet != null)
                    {
                        foreach (var water in Waters.Values)
                        {
                            if (water.PlanetID == WaterMod.Session.ClosestPlanet.EntityId)
                            {
                                water.EnableFish = !water.EnableFish;
                                SyncToServer(true);
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

                    if (WaterMod.Session.ClosestPlanet != null)
                    {
                        foreach (var water in Waters.Values)
                        {
                            if (water.PlanetID == WaterMod.Session.ClosestPlanet.EntityId)
                            {
                                water.EnableFoam = !water.EnableFoam;
                                SyncToServer(true);
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
                        if (WaterMod.Session.ClosestPlanet == null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                            return;
                        }

                        if (WaterMod.Session.ClosestWater != null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GetBuoyancy.Replace("{0}", WaterMod.Session.ClosestWater.Buoyancy.ToString()));
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
                            if (WaterMod.Session.ClosestPlanet == null)
                            {
                                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                                return;
                            }

                            foreach (var water in Waters.Values)
                            {
                                if (water.PlanetID == WaterMod.Session.ClosestPlanet.EntityId)
                                {
                                    water.Buoyancy = MathHelper.Clamp(buoyancy, 0f, 10f);
                                    SyncToServer(true);
                                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetBuoyancy.Replace("{0}", water.Buoyancy.ToString()));
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

                    if (args.Length == 1)
                    {
                        if (WaterMod.Session.ClosestPlanet == null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                            return;
                        }

                        if (WaterMod.Session.ClosestWater != null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GetViscosity.Replace("{0}", (WaterMod.Session.ClosestWater.Material.Viscosity).ToString()));
                            return;
                        }

                        //If foreach loop doesn't find the planet, assume it doesn't exist
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                    }
                    else
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.RetiredCommand);
                    break;

                case "wdensity":
                    sendToOthers = false;

                    if (args.Length == 1)
                    {
                        if (WaterMod.Session.ClosestPlanet == null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                            return;
                        }

                        if (WaterMod.Session.ClosestWater != null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GetDensity.Replace("{0}", (WaterMod.Session.ClosestWater.Material.Density).ToString()));
                            return;
                        }

                        //If foreach loop doesn't find the planet, assume it doesn't exist
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                    }
                    else
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.RetiredCommand);

                    break;
                case "wrradius":
                    sendToOthers = false;

                    //Get Relative Radius
                    if (WaterMod.Session.ClosestPlanet == null)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                        return;
                    }

                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GetRelativeRadius.Replace("{0}", ((Vector3D.Distance(WaterMod.Session.CameraPosition, WaterMod.Session.ClosestPlanet.PositionComp.GetPosition()) - WaterSettings.Default.TideHeight) / WaterMod.Session.ClosestPlanet.MinimumRadius).ToString("0.00000")));

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
                        if (WaterMod.Session.ClosestPlanet == null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                            return;
                        }

                        if (WaterMod.Session.ClosestWater != null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GetRadius.Replace("{0}", (WaterMod.Session.ClosestWater.Radius / WaterMod.Session.ClosestPlanet.MinimumRadius).ToString()));
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

                            if (WaterMod.Session.ClosestPlanet == null)
                            {
                                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                                return;
                            }

                            foreach (var water in Waters.Values)
                            {
                                if (water.PlanetID == WaterMod.Session.ClosestPlanet.EntityId)
                                {

                                    water.Radius = radius * WaterMod.Session.ClosestPlanet.MinimumRadius;
                                    SyncToServer(true);
                                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetRadius.Replace("{0}", (water.Radius / WaterMod.Session.ClosestPlanet.MinimumRadius).ToString()));
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

                case "wcurrentspeed":
                    sendToOthers = false;

                    if (MyAPIGateway.Session.PromoteLevel < MyPromoteLevel.Moderator)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GenericNoPermissions);
                        return;
                    }

                    if (WaterMod.Session.ClosestPlanet == null)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                        return;
                    }

                    //Get Current Speed
                    if (args.Length == 1)
                    {
                        if (WaterMod.Session.ClosestWater != null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GetCurrentSpeed.Replace("{0}", WaterMod.Session.ClosestWater.CurrentSpeed.ToString()));
                            return;
                        }

                        //If foreach loop doesn't find the planet, assume it doesn't exist
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                    }

                    //Set Current Speed
                    if (args.Length == 2)
                    {
                        float currentSpeed;
                        if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out currentSpeed))
                        {
                            foreach (var water in Waters.Values)
                            {
                                if (water.PlanetID == WaterMod.Session.ClosestPlanet.EntityId)
                                {
                                    water.CurrentSpeed = currentSpeed;
                                    SyncToServer(false);
                                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetCurrentSpeed.Replace("{0}", water.CurrentSpeed.ToString()));

                                    return;
                                }
                            }
                            //If foreach loop doesn't find the planet, assume it doesn't exist
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                        }
                        else
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetCurrentSpeedNoParse.Replace("{0}", args[1]));
                    }
                    break;
                case "wcurrentscale":
                    sendToOthers = false;

                    if (MyAPIGateway.Session.PromoteLevel < MyPromoteLevel.Moderator)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GenericNoPermissions);
                        return;
                    }

                    if (WaterMod.Session.ClosestPlanet == null)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                        return;
                    }

                    //Get Current Scale
                    if (args.Length == 1)
                    {
                        if (WaterMod.Session.ClosestWater != null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GetCurrentScale.Replace("{0}", WaterMod.Session.ClosestWater.CurrentScale.ToString()));
                            return;

                        }

                        //If foreach loop doesn't find the planet, assume it doesn't exist
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                    }

                    //Set Current Scale
                    if (args.Length == 2)
                    {
                        float currentScale;
                        if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out currentScale))
                        {
                            foreach (var water in Waters.Values)
                            {
                                if (water.PlanetID == WaterMod.Session.ClosestPlanet.EntityId)
                                {
                                    water.CurrentScale = currentScale;
                                    SyncToServer(false);
                                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetCurrentScale.Replace("{0}", water.CurrentScale.ToString()));

                                    return;
                                }
                            }
                            //If foreach loop doesn't find the planet, assume it doesn't exist
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                        }
                        else
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetCurrentScaleNoParse.Replace("{0}", args[1]));
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
                        if (WaterMod.Session.ClosestPlanet == null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                            return;
                        }

                        if (WaterMod.Session.ClosestWater != null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GetWaveHeight.Replace("{0}", WaterMod.Session.ClosestWater.WaveHeight.ToString()));
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
                            if (WaterMod.Session.ClosestPlanet == null)
                            {
                                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                                return;
                            }

                            foreach (var water in Waters.Values)
                            {
                                if (water.PlanetID == WaterMod.Session.ClosestPlanet.EntityId)
                                {
                                    water.WaveHeight = waveHeight;
                                    SyncToServer(true);
                                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetWaveHeight.Replace("{0}", water.WaveHeight.ToString()));

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
                        if (WaterMod.Session.ClosestPlanet == null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                            return;
                        }

                        if (WaterMod.Session.ClosestWater != null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GetTideHeight.Replace("{0}", WaterMod.Session.ClosestWater.TideHeight.ToString()));
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
                            if (WaterMod.Session.ClosestPlanet == null)
                            {
                                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                                return;
                            }

                            foreach (var water in Waters.Values)
                            {
                                if (water.PlanetID == WaterMod.Session.ClosestPlanet.EntityId)
                                {
                                    water.TideHeight = MyMath.Clamp(tideHeight, 0, 10000);
                                    SyncToServer(true);
                                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetTideHeight.Replace("{0}", water.TideHeight.ToString()));
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
                        if (WaterMod.Session.ClosestPlanet == null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                            return;
                        }

                        if (WaterMod.Session.ClosestWater != null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GetTideSpeed.Replace("{0}", WaterMod.Session.ClosestWater.TideSpeed.ToString()));
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
                            if (WaterMod.Session.ClosestPlanet == null)
                            {
                                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                                return;
                            }

                            foreach (var water in Waters.Values)
                            {
                                if (water.PlanetID == WaterMod.Session.ClosestPlanet.EntityId)
                                {
                                    water.TideSpeed = MyMath.Clamp(tideSpeed, 0, 1000);
                                    SyncToServer(true);
                                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetTideSpeed.Replace("{0}", water.TideSpeed.ToString()));
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

                    if (WaterMod.Session.ClosestPlanet == null)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                        return;
                    }

                    //Reset
                    foreach (var Key in Waters.Keys)
                    {
                        if (Waters[Key].PlanetID == WaterMod.Session.ClosestPlanet.EntityId)
                        {
                            float radius = Waters[Key].Radius;
                            Waters[Key] = new Water(WaterMod.Session.ClosestPlanet){Radius = radius};

                            SyncToServer(true);
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
                        if (WaterMod.Session.ClosestPlanet == null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                            return;
                        }

                        if (WaterMod.Session.ClosestWater != null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GetWaveSpeed.Replace("{0}", WaterMod.Session.ClosestWater.WaveSpeed.ToString()));
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
                            if (WaterMod.Session.ClosestPlanet == null)
                            {
                                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                                return;
                            }

                            foreach (var water in Waters.Values)
                            {
                                if (water.PlanetID == WaterMod.Session.ClosestPlanet.EntityId)
                                {
                                    water.WaveSpeed = MathHelper.Clamp(waveSpeed, 0f, 1f);
                                    SyncToServer(true);
                                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetWaveSpeed.Replace("{0}", water.WaveSpeed.ToString()));
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
                        if (WaterMod.Session.ClosestPlanet == null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                            return;
                        }

                        if (WaterMod.Session.ClosestWater != null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GetWaveScale.Replace("{0}", WaterMod.Session.ClosestWater.WaveScale.ToString()));
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
                            if (WaterMod.Session.ClosestPlanet == null)
                            {
                                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                                return;
                            }

                            foreach (var water in Waters.Values)
                            {
                                if (water.PlanetID == WaterMod.Session.ClosestPlanet.EntityId)
                                {
                                    water.WaveScale = waveScale;
                                    SyncToServer(true);
                                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetWaveScale.Replace("{0}", water.WaveScale.ToString()));
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

                    if (WaterMod.Session.ClosestPlanet == null)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                        return;
                    }

                    if (WaterUtils.HasWater(WaterMod.Session.ClosestPlanet))
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.HasWater);
                        return;
                    }

                    if (args.Length == 2)
                    {
                        float radius;
                        if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out radius))
                        {
                            Waters[WaterMod.Session.ClosestPlanet.EntityId] = new Water(WaterMod.Session.ClosestPlanet, radiusMultiplier: MathHelper.Clamp(radius, 0.01f, 2f));
                            SyncToServer(true);
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
                        Waters[WaterMod.Session.ClosestPlanet.EntityId] = new Water(WaterMod.Session.ClosestPlanet);
                        SyncToServer(true);
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

                    if (WaterMod.Session.ClosestPlanet == null)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                        return;
                    }

                    if (Waters.Remove(WaterMod.Session.ClosestPlanet.EntityId))
                    {
                        SyncToServer(true);
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
                        if (WaterMod.Session.ClosestPlanet == null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                            return;
                        }

                        foreach (var water in Waters.Values)
                        {
                            if (water.PlanetID == WaterMod.Session.ClosestPlanet.EntityId)
                            {
                                water.PlayerDrag = !water.PlayerDrag;
                                SyncToServer(true);
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
                        if (WaterMod.Session.ClosestPlanet == null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                            return;
                        }

                        foreach (var water in Waters.Values)
                        {
                            if (water.PlanetID == WaterMod.Session.ClosestPlanet.EntityId)
                            {
                                water.Transparent = !water.Transparent;
                                SyncToServer(true);
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
                        if (WaterMod.Session.ClosestPlanet == null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                            return;
                        }

                        foreach (var water in Waters.Values)
                        {
                            if (water.PlanetID == WaterMod.Session.ClosestPlanet.EntityId)
                            {
                                water.Lit = !water.Lit;
                                SyncToServer(true);
                                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.ToggleLighting);
                                return;
                            }
                        }

                        //If foreach loop doesn't find the planet, assume it doesn't exist
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                    }
                    break;
                case "wtexturelist": //alias to /wtextures
                case "wtextures":
                    sendToOthers = false;
                    if (WaterData.WaterTextures.Count > 0)
                    {
                        string textures = "";

                        foreach (var texture in WaterData.WaterTextures)
                        {
                            textures += texture + ", ";
                        }

                        if (textures.Length > 2)
                        {
                            textures = textures.Substring(0, textures.Length - 2) + "."; //remove end comma and space. add period
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.ListTextures + textures);
                        }
                    }
                    else
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoTextures);
                    }
                    break;
                case "wmateriallist": //alias to /wmaterials
                case "wmaterials":
                    sendToOthers = false;
                    if (WaterData.MaterialConfigs.Count > 0)
                    {
                        string materials = "";

                        foreach (var material in WaterData.MaterialConfigs)
                        {
                            materials += material.Key + ", ";
                        }

                        if (materials.Length > 2)
                        {
                            materials = materials.Substring(0, materials.Length - 2) + "."; //remove end comma and space. add period
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.ListMaterials + materials);
                        }
                    }
                    else
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoMaterials);
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
                        if (WaterMod.Session.ClosestPlanet == null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                            return;
                        }

                        if (WaterMod.Session.ClosestWater != null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GetTexture.Replace("{0}", WaterMod.Session.ClosestWater.Texture.ToString()));
                            return;
                        }

                        //If foreach loop doesn't find the planet, assume it doesn't exist
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                    }

                    //Set Texture
                    if (args.Length == 2)
                    {
                        if (!WaterData.WaterTextures.Contains(args[1]))
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetTextureNoFind.Replace("{0}", args[1]));
                            return;
                        }

                        MyStringId waterTexture = MyStringId.GetOrCompute(args[1]);
                        if (waterTexture != null)
                        {
                            if (WaterMod.Session.ClosestPlanet == null)
                            {
                                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                                return;
                            }

                            foreach (var water in Waters.Values)
                            {
                                if (water.PlanetID == WaterMod.Session.ClosestPlanet.EntityId)
                                {
                                    water.TextureID = waterTexture;
                                    water.Texture = args[1];
                                    water.UpdateTexture();

                                    SyncToServer(true);
                                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetTexture.Replace("{0}", water.Texture.ToString()));
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
                case "wmaterial":
                    sendToOthers = false;

                    if (MyAPIGateway.Session.PromoteLevel < MyPromoteLevel.Moderator)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GenericNoPermissions);
                        return;
                    }

                    //Get Material
                    if (args.Length == 1)
                    {
                        if (WaterMod.Session.ClosestPlanet == null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                            return;
                        }

                        if (WaterMod.Session.ClosestWater != null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GetMaterial.Replace("{0}", WaterMod.Session.ClosestWater.Material.SubtypeId));
                            return;
                        }

                        //If foreach loop doesn't find the planet, assume it doesn't exist
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                    }

                    //Set Material
                    if (args.Length == 2)
                    {
                        if (!WaterData.MaterialConfigs.ContainsKey(args[1]))
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetMaterialNoFind.Replace("{0}", args[1]));
                            return;
                        }

                        if (WaterMod.Session.ClosestPlanet == null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                            return;
                        }

                        foreach (var water in Waters.Values)
                        {
                            if (water.PlanetID == WaterMod.Session.ClosestPlanet.EntityId)
                            {
                                water.MaterialId = args[1];
                                water.UpdateMaterial();

                                //SyncToServer(true);
                                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetMaterial.Replace("{0}", water.Material.SubtypeId.ToString()));
                                return;
                            }
                        }

                        //If foreach loop doesn't find the planet, assume it doesn't exist
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                    }
                    break;
                case "wexport":
                    sendToOthers = false;

                    if (MyAPIGateway.Session.PromoteLevel < MyPromoteLevel.Moderator)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GenericNoPermissions);
                        return;
                    }

                    if (WaterMod.Session.ClosestPlanet == null)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                        return;
                    }

                    if (WaterMod.Session.ClosestWater != null)
                    {
                        PlanetConfig temp = new PlanetConfig(WaterMod.Session.ClosestWater.planet.Generator.Id.SubtypeName, WaterMod.Session.ClosestWater);
                        MyClipboardHelper.SetClipboard(MyAPIGateway.Utilities.SerializeToXML(temp));
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

                    if (WaterMod.Session.ClosestPlanet == null)
                    {
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                        return;
                    }

                    if (WaterMod.Session.ClosestWater != null)
                    {
                        WaterUtils.ShowMessage(WaterMod.Session.ClosestWater.ToString());
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

                    //Get Crush Damage
                    if (args.Length == 1)
                    {
                        if (WaterMod.Session.ClosestPlanet == null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                            return;
                        }

                        if (WaterMod.Session.ClosestWater != null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GetCrushDamage.Replace("{0}", WaterMod.Session.ClosestWater.CrushDamage.ToString()));
                            return;
                        }

                        //If foreach loop doesn't find the planet, assume it doesn't exist
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                    }

                    //Set Crush Damage
                    if (args.Length == 2)
                    {
                        float crushDepth;
                        if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out crushDepth))
                        {
                            if (WaterMod.Session.ClosestPlanet == null)
                            {
                                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                                return;
                            }

                            foreach (var water in Waters.Values)
                            {
                                if (water.PlanetID == WaterMod.Session.ClosestPlanet.EntityId)
                                {
                                    water.CrushDamage = crushDepth;
                                    SyncToServer(true);
                                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetCrushDamage.Replace("{0}", water.CrushDamage.ToString()));
                                    return;
                                }
                            }

                            //If foreach loop doesn't find the planet, assume it doesn't exist
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                        }
                        else
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetCrushDamageNoParse.Replace("{0}", args[1]));
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
                        if (WaterMod.Session.ClosestPlanet == null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                            return;
                        }

                        if (WaterMod.Session.ClosestWater != null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GetCollectRate.Replace("{0}", WaterMod.Session.ClosestWater.CollectionRate.ToString()));
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
                            if (WaterMod.Session.ClosestPlanet == null)
                            {
                                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                                return;
                            }

                            foreach (var water in Waters.Values)
                            {
                                if (water.PlanetID == WaterMod.Session.ClosestPlanet.EntityId)
                                {
                                    water.CollectionRate = collectRate;
                                    SyncToServer(true);
                                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetCollectRate.Replace("{0}", water.CollectionRate.ToString()));
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
                        if (WaterMod.Session.ClosestPlanet == null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                            return;
                        }

                        if (WaterMod.Session.ClosestWater != null)
                        {
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GetFogColor.Replace("{0}", WaterMod.Session.ClosestWater.FogColor.ToString()));
                            return;
                        }

                        //If foreach loop doesn't find the planet, assume it doesn't exist
                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                    }

                    //Set Fog Color
                    if (args.Length == 4)
                    {
                        float r, g, b;
                        if (float.TryParse(WaterUtils.ValidateCommandData(args[1]), out r))
                        {
                            if (float.TryParse(WaterUtils.ValidateCommandData(args[2]), out g))
                            {
                                if (float.TryParse(WaterUtils.ValidateCommandData(args[3]), out b))
                                {
                                    if (WaterMod.Session.ClosestPlanet == null)
                                    {
                                        WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                                        return;
                                    }

                                    foreach (var water in Waters.Values)
                                    {
                                        if (water.PlanetID == WaterMod.Session.ClosestPlanet.EntityId)
                                        {
                                            water.FogColor = new Vector3(r, g, b);
                                            SyncToServer(true);
                                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetFogColor.Replace("{0}", water.FogColor.ToString()));
                                            return;
                                        }
                                    }

                                    //If foreach loop doesn't find the planet, assume it doesn't exist
                                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                                }
                                else
                                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetCollectRateNoParse.Replace("{0}", args[3]));
                            }
                            else
                                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetCollectRateNoParse.Replace("{0}", args[2]));
                        }
                        else
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetCollectRateNoParse.Replace("{0}", args[1]));
                    }
                    break;
            }
        }

        #endregion Commands

        /// <summary>
        /// Called when the world is loading
        /// </summary>
        public override void LoadData()
        {
            WeaponCoreAPI.Load();
            WaterModAPIBackend.LoadData();

            //Calculate volume of every block in m^3
            foreach (var definition in MyDefinitionManager.Static.GetAllDefinitions())
            {
                if (!WaterData.CharacterConfigs.ContainsKey(definition.Id))
                    if (definition is MyCharacterDefinition)
                    {
                        MyCharacterDefinition characterDefinition = (MyCharacterDefinition)definition;
                        WaterData.CharacterConfigs[characterDefinition.Id] = new CharacterConfig()
                        {
                            Volume = characterDefinition.CharacterLength * characterDefinition.CharacterWidth * characterDefinition.CharacterHeight,
                        };
                    }

                if (definition is MyCubeBlockDefinition)
                {
                    //Light Armor Block is 2.5x2.5x2.5 and weighs 500kg
                    //Density = Mass / Volume
                    //Density = 500kg / (2.5*2.5*2.5)
                    //Density = 32kg/m^3
                    //Volume = Mass / Density
                    //Volume = Block Mass / 32kg

                    //Heavy Armor Block is 2.5x2.5x2.5 and weighs 3300kg
                    //Density = Mass / Volume
                    //Density = 3300kg / (2.5*2.5*2.5)
                    //Density = 211.2kg/m^3
                    //Volume = Mass / Density
                    //Volume = Block Mass / 211.2kg

                    //Catwalk is 193kg and has 3.125m^3 Density of 61.76
                    //Window is 435kg and has 3.125m^3 Density of 139.2
                    //Passage is 574kg and has 7.03125m^3 Density of 81.6
                    //Conveyor is 394kg and has 4.375m^3 Density of 90.1
                    //Piston top is 400kg and has 3.5m^3 Density of 114.3
                    //Desk is 330kg and has 1.1m^3 Density of 300
                    //Solar Panel is 516kg and has 25m^3 Density of 20.64
                    //Text Panel is 222kg and has 3.125m^3 Density of 71
                    //Cover Block is 160kg and has 1.25m^3 Density of 128
                    //Neon tube is 58kg and has 0.3125m^3 Density of 185.6
                    //Beam Block is 500kg and has 8.125m^3 Density of 61.5
                    //Sliding Door is 1065kg and has 8.125m^3 Density of 131.1

                    MyCubeBlockDefinition blockDefinition = (MyCubeBlockDefinition)definition;

                    if (blockDefinition.Enabled && blockDefinition.Id != null)
                    {
                        float blockSize = ((blockDefinition.CubeSize == MyCubeSize.Large) ? 2.5f : 0.5f);
                        Vector3 blockDimensions = blockDefinition.Size * blockSize;

                        BlockConfig Config;

                        if (!WaterData.BlockConfigs.TryGetValue(definition.Id, out Config) || Config.Volume < 0) //If it could not find the definition or the volume isn't set
                        {
                            if (Config == null)
                                Config = WaterData.BlockConfigs[blockDefinition.Id] = new BlockConfig()
                                {
                                    DefinitionId = blockDefinition.Id
                                };

                            if (blockDefinition.IsAirTight == true)
                                Config.Volume = blockDimensions.Volume; //IsAirtight implies that every side is a solid wall, meaning that 100% of water in the block is displaced
                            else if (!blockDefinition.HasPhysics)
                                Config.Volume = 0; //Block is negligibly small (Control panel, Light, etc...)
                            else if (blockDefinition.MountPoints == null)
                                Config.Volume = blockDimensions.Volume; //No mount points means you can place it on any side, meaning that 100% of water is displaced
                            else if (blockDefinition.DescriptionEnum?.String == "Description_LightArmor")
                            {
                                Config.Volume = Math.Min(blockDefinition.Mass / 32f, (blockDimensions.Volume)); //Light Armor has a uniform density
                                Config.MaximumPressure = 4014.08f;
                            }
                            else if (blockDefinition.DescriptionEnum?.String == "Description_HeavyArmor")
                            {
                                Config.Volume = Math.Min(blockDefinition.Mass / 211.2f, (blockDimensions.Volume)); //Heavy Armor can assume uniform density
                                Config.MaximumPressure = 6021.12f;
                            }
                            else if (blockDefinition.DescriptionEnum?.String == "Description_DeadEngineer")
                                Config.Volume = 0; //Don't bother with dead engineer
                            else if (blockDefinition.DescriptionEnum?.String == "Description_ButtonPanel")
                                Config.Volume = 0; //Don't bother with button panels
                            else if (blockDefinition.DescriptionEnum?.String == "Description_Wheel")
                                Config.Volume = (float)(Math.PI * Math.Pow(blockDimensions.Max() / 2f, 2f) * blockDimensions.Min()); //Wheels are cylinders and only the diameter ever changes. v = π(r^2)h
                            else if (blockDefinition.DescriptionEnum?.String == "Description_SteelCatwalk")
                                Config.Volume = Math.Min(blockDefinition.Mass / 61.76f, (blockDimensions.Volume)); //Catwalk can assume uniform density
                            else if (blockDefinition.DescriptionEnum?.String == "Description_GratedCatwalk")
                                Config.Volume = Math.Min(blockDefinition.Mass / 61.76f, (blockDimensions.Volume)); //Catwalk can assume uniform density
                            else if (blockDefinition.DescriptionEnum?.String == "Description_GratedCatwalkCorner")
                                Config.Volume = Math.Min(blockDefinition.Mass / 61.76f, (blockDimensions.Volume)); //Catwalk can assume uniform density
                            else if (blockDefinition.DescriptionEnum?.String == "Description_GratedCatwalkStraight")
                                Config.Volume = Math.Min(blockDefinition.Mass / 61.76f, (blockDimensions.Volume)); //Catwalk can assume uniform density
                            else if (blockDefinition.DescriptionEnum?.String == "Description_GratedCatwalkWall")
                                Config.Volume = Math.Min(blockDefinition.Mass / 61.76f, (blockDimensions.Volume)); //Catwalk can assume uniform density
                            else if (blockDefinition.DescriptionEnum?.String == "Description_Ramp")
                                Config.Volume = (blockDimensions.X * blockDimensions.Y * blockDimensions.Z) / 2; //Ramps are literally just half the block space it takes up
                            else if (blockDefinition is MyGasTankDefinition)
                            {
                                Config.Volume = (float)(Math.PI * Math.Pow(blockDimensions.Min() / 2f, 2f) * (blockDimensions.Max())); //Tanks are generally cylinders. v = πr2h
                                Config.IsPressurized = true;
                            }
                            else if (blockDefinition is MyThrustDefinition)
                                Config.Volume = (float)(Math.PI * Math.Pow(blockDimensions.Min() / 2f, 2f) * (blockDimensions.Max())); //Thrusters are generally cylinders. v = πr2h
                            else if (blockDefinition is MyPistonBaseDefinition)
                                Config.Volume = (float)(Math.PI * Math.Pow(blockDimensions.Min() / 2f, 2f) * (blockDimensions.Max())); //Pistons are generally cylinders. v = πr2h
                            else if (blockDefinition.DescriptionEnum?.String == "Description_VerticalWindow")
                                Config.Volume = Math.Min(blockDefinition.Mass / 139.2f, (blockDimensions.Volume)); //Window can assume uniform density (glass)
                            else if (blockDefinition.DescriptionEnum?.String == "Description_DiagonalWindow")
                                Config.Volume = Math.Min(blockDefinition.Mass / 139.2f, (blockDimensions.Volume)); //Window can assume uniform density (glass)
                            else if (blockDefinition.DescriptionEnum?.String == "Description_Window")
                                Config.Volume = Math.Min(blockDefinition.Mass / 139.2f, (blockDimensions.Volume)); //Window can assume uniform density (glass)
                            else if (blockDefinition.DescriptionEnum?.String == "Description_Letters")
                                Config.Volume = 0; //Letter blocks are really small, don't bother
                            else if (blockDefinition.DescriptionEnum?.String == "Description_Symbols")
                                Config.Volume = 0; //Symbol blocks are really small, don't bother
                            else if (blockDefinition is MyGravityGeneratorBaseDefinition)
                                Config.Volume = (float)((4f / 3f) * Math.PI * (blockDimensions.X / 2) * (blockDimensions.Y / 2) * (blockDimensions.Z / 2)); //Gravity Generators are pretty much spheres v = (4/3)πr3
                            else if (blockDefinition is MyReactorDefinition)
                                Config.Volume = (float)((4f / 3f) * Math.PI * (blockDimensions.X / 2) * (blockDimensions.Y / 2) * (blockDimensions.Z / 2)); //Reactors are pretty much spheres v = (4/3)πr3
                            else if (blockDefinition is MyDecoyDefinition)
                                Config.Volume = (float)((4f / 3f) * Math.PI * (blockDimensions.X / 2) * (blockDimensions.Y / 2) * (blockDimensions.Z / 2)); //Decoys are pretty much spheres v = (4/3)πr3
                            else if (blockDefinition is MyWarheadDefinition)
                                Config.Volume = (float)((4f / 3f) * Math.PI * (blockDimensions.X / 2) * (blockDimensions.Y / 2) * (blockDimensions.Z / 2)); //Warheads are pretty much spheres v = (4/3)πr3
                            else if (blockDefinition is MyGyroDefinition)
                                Config.Volume = (float)((4f / 3f) * Math.PI * (blockDimensions.X / 2) * (blockDimensions.Y / 2) * (blockDimensions.Z / 2)); //Gyroscopes are pretty much spheres v = (4/3)πr3
                            else if (blockDefinition is MyConveyorSorterDefinition)
                                Config.Volume = Math.Min(blockDefinition.Mass / 90.1f, (blockDimensions.Volume)); //Conveyors can assume uniform density
                            else if (blockDefinition is MySolarPanelDefinition)
                                Config.Volume = Math.Min(blockDefinition.Mass / 20.64f, (blockDimensions.Volume)); //Solar Panels can assume uniform density
                            else if (blockDefinition is MyTextPanelDefinition)
                                Config.Volume = Math.Min(blockDefinition.Mass / 71f, (blockDimensions.Volume)); //Text Panels can assume uniform density
                            else if (blockDefinition is MyShipToolDefinition)
                                Config.Volume = blockDimensions.Volume / 2; //Ship tools are generally one full block and some extra, I'll just account for one block
                            else if (blockDefinition.DescriptionEnum?.String == "Description_Passage")
                                Config.Volume = Math.Min(blockDefinition.Mass / 81.6f, (blockDimensions.Volume)); //Passage can assume uniform density
                            else if (blockDefinition.DescriptionEnum?.String == "Description_Conveyor")
                                Config.Volume = Math.Min(blockDefinition.Mass / 90.1f, (blockDimensions.Volume)); //Conveyors can assume uniform density
                            else if (blockDefinition.DescriptionEnum?.String == "Description_PistonTop")
                                Config.Volume = Math.Min(blockDefinition.Mass / 114.3f, (blockDimensions.Volume)); //Piston top can assume uniform density
                            else if (blockDefinition.DescriptionEnum?.String == "Description_Desk")
                                Config.Volume = Math.Min(blockDefinition.Mass / 300f, (blockDimensions.Volume)); //Desks can assume uniform density
                            else if (blockDefinition.DescriptionEnum?.String == "Description_FullCoverWall")
                                Config.Volume = Math.Min(blockDefinition.Mass / 128f, (blockDimensions.Volume)); //Cover Walls can assume uniform density
                            else if (blockDefinition.DescriptionEnum?.String == "Description_FullCoverWall")
                                Config.Volume = Math.Min(blockDefinition.Mass / 128f, (blockDimensions.Volume)); //Cover Walls can assume uniform density
                            else if (blockDefinition.DescriptionEnum?.String == "Description_RailingStraight")
                                Config.Volume = Math.Min(blockDefinition.Mass / 128f, (blockDimensions.Volume)); //Cover Walls can assume uniform density
                            else if (blockDefinition.DescriptionEnum?.String == "Description_RailingDouble")
                                Config.Volume = Math.Min(blockDefinition.Mass / 128f, (blockDimensions.Volume)); //Cover Walls can assume uniform density
                            else if (blockDefinition.DescriptionEnum?.String == "Description_RailingCorner")
                                Config.Volume = Math.Min(blockDefinition.Mass / 128f, (blockDimensions.Volume)); //Cover Walls can assume uniform density
                            else if (blockDefinition.DescriptionEnum?.String == "Description_RailingDiagonal")
                                Config.Volume = Math.Min(blockDefinition.Mass / 128f, (blockDimensions.Volume)); //Cover Walls can assume uniform density
                            else if (blockDefinition.DescriptionEnum?.String == "Description_NeonTubes")
                                Config.Volume = Math.Min(blockDefinition.Mass / 185.6f, (blockDimensions.Volume)); //Neon Tubes can assume uniform density
                            else if (blockDefinition.DescriptionEnum?.String == "Description_BeamBlock")
                                Config.Volume = Math.Min(blockDefinition.Mass / 61.5f, (blockDimensions.Volume)); //Neon Tubes can assume uniform density
                            else if (blockDefinition is MyCargoContainerDefinition)
                                Config.Volume = blockDimensions.Volume; //Cargo Containers are cubes
                            else if (blockDefinition is MyAssemblerDefinition)
                                Config.Volume = blockDimensions.Volume; //Assemblers are cubes
                            else if (blockDefinition is MyJumpDriveDefinition)
                                Config.Volume = blockDimensions.Volume; //Jump Drives are cubes
                            else if (blockDefinition is MyBatteryBlockDefinition)
                                Config.Volume = blockDimensions.Volume; //Batteries are cubes
                            else if (blockDefinition is MyAirtightHangarDoorDefinition)
                                Config.Volume = (blockDimensions.X * blockDimensions.Y * blockDimensions.Z) / 2; //Hangar Doors are somewhat slopes
                            else if (blockDefinition is MyAirtightSlideDoorDefinition)
                                Config.Volume = Math.Min(blockDefinition.Mass / 131.1f, blockDimensions.Volume); //Sliding Doors assume uniform density
                            else if (blockDefinition is MyAirtightDoorGenericDefinition)
                                Config.Volume = Math.Min(blockDefinition.Mass / 131.1f, blockDimensions.Volume); //Doors assume uniform density
                            else if (blockDefinition is MyAdvancedDoorDefinition)
                                Config.Volume = Math.Min(blockDefinition.Mass / 131.1f, blockDimensions.Volume); //Doors assume uniform density
                            else if (blockDefinition is MyDoorDefinition)
                                Config.Volume = Math.Min(blockDefinition.Mass / 131.1f, blockDimensions.Volume); //Doors assume uniform density
                            else if (blockDefinition is MyRefineryDefinition)
                                Config.Volume = (float)(Math.PI * Math.Pow(blockDimensions.Min() / 2f, 2f) * (blockDimensions.Max())); //Refinerys are cylinders
                            else if (blockDefinition is MyCockpitDefinition)
                            {
                                Config.Volume = (blockDimensions.X * blockDimensions.Y * blockDimensions.Z) / 2; //Assume cockpits are similar to ramp shape
                                Config.IsPressurized = (blockDefinition as MyCockpitDefinition).IsPressurized;
                            }
                            else if (blockDefinition.DescriptionEnum?.String == "Description_ViewPort")
                                Config.Volume = (blockDimensions.X * blockDimensions.Y * blockDimensions.Z) / 2; //Viewports are half blocks
                            else if (blockDefinition.DescriptionEnum?.String == "Description_StorageShelf")
                                Config.Volume = (blockDimensions.X * blockDimensions.Y * blockDimensions.Z) * (2 / 5); //Shelfs are weird so I'll just approximate
                            else if (blockDefinition.DescriptionEnum?.String == "Description_WeaponRack")
                                Config.Volume = (blockDimensions.X * blockDimensions.Y * blockDimensions.Z) * (2 / 5); //Weapon Racks are weird so I'll just approximate
                            else
                            {
                                Config.Volume = Math.Min(blockDefinition.Mass / 32f, (blockDimensions.Volume)); //No more information, treat it like a steel block
                            }

                            Config.Init();
                        }

                        Config.Volume = Math.Max(Math.Min(Config.Volume, blockDimensions.Volume), 0); //Clamp volume to never be greater than the possible volume

                        //Block Description Info
                        if (Config.Volume > (blockDefinition.CubeSize == MyCubeSize.Large ? WaterData.MinVolumeLarge : WaterData.MinVolumeSmall))
                        {
                            blockDefinition.DescriptionString = blockDefinition.DescriptionText + "\n" +
                                "\nBlock Volume: " + Config.Volume.ToString("0.00") + "m³";
                            blockDefinition.DescriptionEnum = null;
                        }
                        else
                        {
                            blockDefinition.DescriptionString = blockDefinition.DescriptionText + "\n\nBlock Volume: Not Simulated";
                            blockDefinition.DescriptionEnum = null;
                        }

                        if (blockDefinition is MyFunctionalBlockDefinition && !(blockDefinition is MyShipControllerDefinition) && !(blockDefinition is MyPlanterDefinition) && !(blockDefinition is MyKitchenDefinition))
                        {
                            blockDefinition.DescriptionString += "\nFunctional Underwater: " + Config.FunctionalUnderWater;
                            blockDefinition.DescriptionString += "\nFunctional Abovewater: " + Config.FunctionalAboveWater;
                        }
                    }
                }
            }

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
                        Waters[water.PlanetID] = water;
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
                water.UpdateMaterial();
            }

            LoadSettings();

            //Draygo
            DragAPI.Init();
        }

        //spaghetti
        public void LoadSettings()
        {
            //this is spaghetti :(
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
                try
                {
                    System.IO.TextWriter writer =
                        MyAPIGateway.Utilities.WriteFileInGlobalStorage("JakariaWaterSettings.cfg");
                    writer.WriteLine(Settings.Quality);
                    writer.WriteLine(WaterLocalization.CurrentLanguage.EnglishName);
                    writer.WriteLine(Settings.ShowDepth);
                    writer.WriteLine(Settings.Volume);
                    writer.WriteLine(Settings.ShowAltitude);
                    writer.Close();
                }
                catch (Exception e)
                {
                    WaterUtils.ShowMessage(e.ToString());
                    WaterUtils.WriteLog(e.ToString());
                }
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
            MyExplosions.OnExplosion -= MyExplosions_OnExplosion;
            MyEntities.OnEntityAdd -= Entities_OnEntityCreate;

            if (MyAPIGateway.Session.IsServer)
            {
                MyVisualScriptLogicProvider.PlayerConnected -= PlayerConnected;
                MyVisualScriptLogicProvider.PlayerConnected -= PlayerDisconnected;
                MyEntities.OnEntityAdd -= Entities_OnEntityAdd;
                MyVisualScriptLogicProvider.RespawnShipSpawned -= RespawnShipSpawned;
            }

            EnvironmentUnderwaterSoundEmitter.Cleanup();
            EnvironmentUndergroundSoundEmitter.Cleanup();
            EnvironmentOceanSoundEmitter.Cleanup();
            EnvironmentBeachSoundEmitter.Cleanup();
            AmbientSoundEmitter.Cleanup();
            AmbientBoatSoundEmitter.Cleanup();
        }

        private void Entities_OnEntityCreate(MyEntity entity)
        {
            if (entity.IsPreview || entity.Closed)
                return;

            if (entity is IMyCharacter && !entity.Components.Has<WaterPhysicsComponentCharacter>())
            {
                entity.Components.Add(new WaterPhysicsComponentCharacter());
            }

            if (entity is IMyFloatingObject && !entity.Components.Has<WaterPhysicsComponentFloatingObject>())
            {
                entity.Components.Add(new WaterPhysicsComponentFloatingObject());
            }

            if (entity is MyCubeGrid && !entity.Components.Has<WaterPhysicsComponentGrid>())
            {
                entity.Components.Add(new WaterPhysicsComponentGrid());
            }
        }

        private void RespawnShipSpawned(long shipEntityId, long playerId, string respawnShipPrefabName)
        {
            IMyCubeGrid Grid = MyEntities.GetEntityById(shipEntityId) as IMyCubeGrid;

            if (Grid != null)
            {
                Vector3D position = Grid.PositionComp.GetPosition();
                Vector3D spawnPosition;
                Vector3D surfacePosition;

                Vector3D normal;
                MyPlanet planet = MyGamePruningStructure.GetClosestPlanet(position);
                Water water = GetClosestWater(position);

                RespawnPodConfig config;
                if (water != null && planet != null && WaterData.RespawnPodConfigs.TryGetValue(MyDefinitionId.Parse("RespawnShipDefinition/" + respawnShipPrefabName), out config))
                {
                    MyAPIGateway.Parallel.Start(() =>
                    {
                        for (int i = 0; i < 1000; i++)
                        {
                            surfacePosition = planet.GetClosestSurfacePointGlobal(Vector3.Normalize(MyUtils.GetRandomVector3D()) * water.Radius);
                            spawnPosition = water.GetClosestSurfacePointSimple(surfacePosition);
                            normal = water.GetUpDirection(ref spawnPosition);

                            if (water.IsUnderwater(ref spawnPosition))
                            {
                                if (config.SpawnOnWater)
                                {
                                    spawnPosition += (normal * config.WaterSpawnAltitude);

                                    if (!WaterUtils.IsUnderGround(planet, spawnPosition))
                                    {
                                        MyAPIGateway.Utilities.InvokeOnGameThread(() => Grid.SetPosition(spawnPosition));

                                        return;
                                    }
                                }
                            }
                            else
                            {
                                if (!config.SpawnOnWater)
                                {
                                    spawnPosition = surfacePosition + (normal * Math.Abs(config.WaterSpawnAltitude));

                                    MyAPIGateway.Utilities.InvokeOnGameThread(() => Grid.SetPosition(spawnPosition));
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
            tickTimerSecond--;
            if (tickTimerSecond <= 0)
            {
                UpdateAfterSecond();
                tickTimerSecond = 60;
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

                //Client
                if (!MyAPIGateway.Utilities.IsDedicated)
                {
                    if (WaterMod.Session.ClosestPlanet != null)
                        nightValue = MyMath.Clamp(Vector3.Dot(-Session.GravityDirection, WaterMod.Session.SunDirection) + 0.22f, 0.22f, 1f);

                    Session.SunDirection = MyVisualScriptLogicProvider.GetSunDirection();
                }
            }

            SimulatePhysics();

            UpdateAfter1?.Invoke();
            Session.SessionTimer++;

            //Clients
            if (!MyAPIGateway.Utilities.IsDedicated && initialized && MyAPIGateway.Session != null && MyAPIGateway.Session.Player != null && MyAPIGateway.Session.Camera != null)
            {

                if (Settings.Volume != 0)
                    SimulateSounds();

                Session.InsideGrid = MyAPIGateway.Session?.Player?.Character?.Components?.Get<MyEntityReverbDetectorComponent>() != null ? MyAPIGateway.Session.Player.Character.Components.Get<MyEntityReverbDetectorComponent>().Grids : 0;
                Session.InsideVoxel = MyAPIGateway.Session?.Player?.Character?.Components?.Get<MyEntityReverbDetectorComponent>() != null ? MyAPIGateway.Session.Player.Character.Components.Get<MyEntityReverbDetectorComponent>().Voxels : 0;

                Session.CameraAirtight = WaterUtils.IsPositionAirtight(ref WaterMod.Session.CameraPosition);

                if (Session.CameraAirtight || ((MyAPIGateway.Session.Player.Controller?.ControlledEntity?.Entity as MyCockpit) != null && (MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity as MyCockpit).OxygenFillLevel > 0f))
                    Session.InsideGrid = 25;

                if (MyAPIGateway.Input.IsNewGameControlPressed(MyControlsSpace.TOGGLE_HUD))
                    Settings.ShowHud = !MyAPIGateway.Session?.Config?.MinimalHud ?? true;

                WaterMod.Session.ClosestPlanet = MyGamePruningStructure.GetClosestPlanet(Session.CameraPosition);
                WaterMod.Session.ClosestWater = GetClosestWater(Session.CameraPosition);

                if (WaterMod.Session.ClosestPlanet != null)
                {
                    Session.GravityDirection = Vector3.Normalize(WaterMod.Session.ClosestPlanet.PositionComp.GetPosition() - Session.CameraPosition);

                    Session.Gravity = Session.GravityDirection * WaterMod.Session.ClosestPlanet.Generator.SurfaceGravity;
                }

                if (WaterMod.Session.ClosestWater != null)
                    MyAPIGateway.Parallel.StartBackground(SimulateEffects);

                DrawAfterSimulation();
            }

        }

        public void RebuildLOD()
        {
            Session.LastLODBuildPosition = Session.CameraPosition;

            if (WaterMod.Session.ClosestPlanet != null)
            {
                Session.CameraAltitude = WaterMod.Session.ClosestPlanet != null ? WaterUtils.GetAltitude(WaterMod.Session.ClosestPlanet, Session.CameraPosition) : double.MaxValue;
            }
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

                    //water.waterFaces[i].ConstructTree();
                    water.waterFaces[i].Tree = null;
                }
            }
        }

        private void DrawAfterSimulation()
        {
            Vector4 WhiteColor = new Vector4(nightValue, nightValue, nightValue, 1);
            QuadBillboards?.Clear();
            BillboardCache?.Clear();

            //Hot Tubs
            foreach (var Tub in WaterMod.Static.HotTubs)
            {
                if (Tub == null)
                    continue;
                //todo reimplement drawing
                if (Tub.underWater && !Tub.airtight)
                    continue;

                MatrixD Matrix = Tub.Block.PositionComp.WorldMatrixRef;
                Vector4 Color = WaterData.WaterColor * MyMath.Clamp(Vector3.Dot((Vector3)Matrix.Up, Session.SunDirection) + 0.1f, 0.05f, 1f);

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

            MyAPIGateway.Parallel.ForEach(Waters.Values, water =>
            {
                if (water.PlanetConfig == null || water.Material == null)
                    water.UpdateTexture();

                if (water?.waterFaces != null)
                    foreach (var face in water.waterFaces)
                    {
                        if (face?.Water == null)
                            continue;

                        face.Draw(WaterMod.Session.ClosestWater?.PlanetID == water.PlanetID);
                    }
            });

            //Effects
            lock (effectLock)
            {
                float lifeRatio = 0;

                if (!Session.CameraUnderwater)
                {
                    float size;
                    MyQuadD Quad;
                    Vector3D axisA;
                    Vector3D axisB;

                    foreach (var splash in SurfaceSplashes)
                    {
                        if (splash == null)
                            continue;

                        lifeRatio = (float)splash.life / splash.maxLife;
                        size = splash.radius * lifeRatio;
                        axisA = Session.GravityAxisA * size;
                        axisB = Session.GravityAxisB * size;
                        Quad.Point0 = WaterMod.Session.ClosestWater.GetClosestSurfacePoint(splash.position - axisA - axisB);
                        Quad.Point2 = WaterMod.Session.ClosestWater.GetClosestSurfacePoint(splash.position + axisA + axisB);
                        Quad.Point1 = WaterMod.Session.ClosestWater.GetClosestSurfacePoint(splash.position - axisA + axisB);
                        Quad.Point3 = WaterMod.Session.ClosestWater.GetClosestSurfacePoint(splash.position + axisA - axisB);

                        QuadBillboards.Push(new QuadBillboard(WaterData.SplashMaterial, ref Quad, WhiteColor * (1f - lifeRatio) * Session.ClosestWater.PlanetConfig.ColorIntensity));
                    }
                }
            }
        }

        /// <summary>
        /// Called once every second (60 ticks) after the simulation has run
        /// </summary>
        private void UpdateAfterSecond()
        {
            UpdateAfter60?.Invoke();

            try
            {
                players.Clear();
                MyAPIGateway.Players.GetPlayers(players);

                //Clients
                if (!MyAPIGateway.Utilities.IsDedicated)
                {
                    if (Settings.ShowDebug && MyAPIGateway.Session?.Player?.Character != null)
                    {
                        MyAPIGateway.Utilities.ShowNotification("QuadBillboards " + QuadBillboards.Count, 16 * 60);
                        MyAPIGateway.Utilities.ShowNotification("BillboardCache " + BillboardCache.Count, 16 * 60);
                        MyAPIGateway.Utilities.ShowNotification("SimulatedSplashes " + SimulatedSplashes.Count, 16 * 60);
                        MyAPIGateway.Utilities.ShowNotification("SurfaceSplashes " + SurfaceSplashes.Count, 16 * 60);
                        MyAPIGateway.Utilities.ShowNotification("DamageQueue " + DamageQueue.Count, 16 * 60);
                        MyAPIGateway.Utilities.ShowNotification("Bubbles " + Bubbles.Count, 16 * 60);
                        MyAPIGateway.Utilities.ShowNotification("Waters " + Waters.Count, 16 * 60);
                    }

                    Session.CameraClosestWaterPosition = WaterMod.Session.ClosestWater != null ? WaterMod.Session.ClosestWater.GetClosestSurfacePoint(Session.CameraPosition) : Vector3D.Zero;
                    Session.CameraClosestPosition = WaterMod.Session.ClosestPlanet?.GetClosestSurfacePointGlobal(Session.CameraPosition) ?? Vector3D.Zero;
                    Session.CameraAltitude = WaterMod.Session.ClosestPlanet != null ? WaterUtils.GetAltitude(WaterMod.Session.ClosestPlanet, Session.CameraPosition) : double.MaxValue;
                    Session.CameraUnderground = Session.CameraAltitude < 0;
                    Session.CameraAboveWater = WaterMod.Session.ClosestWater == null || Session.CameraDepth > 0;
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
        /// Creates a bubble (for underwater only)
        /// </summary>
        public void CreateBubble(ref Vector3D position, float radius)
        {
            if (MyAPIGateway.Utilities.IsDedicated || Session.ClosestWater?.Material?.DrawBubbles != true)
                return;

            lock (effectLock)
                Bubbles.Add(new AnimatedPointBillboard(position, -WaterMod.Session.GravityDirection, radius, MyUtils.GetRandomInt(250, 500), MyUtils.GetRandomFloat(0, 360), WaterData.BubblesMaterial));
        }

        public void CreateSplash(Vector3D Position, float Radius, bool Audible)
        {
            if (MyAPIGateway.Utilities.IsDedicated || Session.ClosestWater?.Material?.DrawSplashes != true)
                return;

            lock (effectLock)
                SurfaceSplashes.Add(new Splash(Position, Radius, Audible ? 1 : 0));
        }

        public void CreatePhysicsSplash(Vector3D Position, Vector3D Velocity, float Radius, float Variation, int Count = 1)
        {
            for (int i = 0; i < Count; i++)
            {
                Vector3 randomHemisphere = MyUtils.GetRandomVector3HemisphereNormalized(Vector3.Normalize(Velocity)) * 20;
                float rand = MyUtils.GetRandomFloat(-1, 1);
                SimulatedSplashes.Add(new SimulatedSplash(Position + (randomHemisphere / 2), (Velocity + randomHemisphere * (rand * Variation)) / 3, Radius + rand + 1, Session.ClosestWater));
            }
        }

        /// <summary>
        /// Simulates water physics
        /// </summary>
        public void SimulatePhysics()
        {
            foreach (var water in Waters.Values)
            {
                water.Simulate();
            }

            if (MyAPIGateway.Session.IsServer)
            {
                foreach (var Block in DamageQueue) //Damaging blocks is not thread-safe so I do it here
                {
                    if (Block.Item1?.CubeGrid == null || Block.Item1.CubeGrid.MarkedForClose == true)
                        continue;

                    if (Block.Item1 != null)
                    {
                        Block.Item1.DoDamage(Block.Item2, MyDamageType.Fall, true);
                    }
                }
                DamageQueue.Clear();
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

        /// <summary>
        /// Draw
        /// </summary>
        public override void Draw()
        {
            //Vector4 WhiteColor = new Vector4(nightValue, nightValue, nightValue, 1);

            if (MyAPIGateway.Session.Camera?.WorldMatrix != null)
            {
                Session.CameraPosition = MyAPIGateway.Session.Camera.Position;
                Session.CameraRotation = MyAPIGateway.Session.Camera.WorldMatrix.Forward;
            }

            if (Session.GravityDirection != Vector3D.Zero)
            {
                Session.GravityAxisA = Vector3D.CalculatePerpendicularVector(Session.GravityDirection);
                Session.GravityAxisB = Session.GravityAxisA.Cross(Session.GravityDirection);
            }

            if (WaterMod.Session.ClosestWater != null)
            {
                Session.DistanceToHorizon = Session.CameraUnderwater ? 250 : (float)Math.Max(Math.Sqrt((Math.Pow(Math.Max(Session.CameraDepth, 50) + WaterMod.Session.ClosestWater.Radius, 2)) - (WaterMod.Session.ClosestWater.Radius * WaterMod.Session.ClosestWater.Radius)), 500);

                if (sunOccluder != null)
                {
                    sunOccluder.Render.Visible = Session.CameraUnderwater || (WaterMod.Session.ClosestWater.Intersects(Session.CameraPosition, Session.CameraPosition + (Session.SunDirection * 100)) != 0);

                    if (sunOccluder.Render.Visible)
                        sunOccluder.WorldMatrix = MatrixD.CreateWorld(Session.CameraPosition + (Session.SunDirection * 1000), -Vector3.CalculatePerpendicularVector(Session.SunDirection), -Session.SunDirection);
                }

                if (particleOccluder != null)
                {
                    if (Session.CameraUnderwater)
                        particleOccluder.WorldMatrix = MatrixD.CreateWorld(Session.CameraPosition + (Session.CameraRotation * 250), -Vector3D.CalculatePerpendicularVector(Session.CameraRotation), -Session.CameraRotation);
                    else
                        particleOccluder.WorldMatrix = MatrixD.CreateWorld(WaterMod.Session.ClosestWater.GetClosestSurfacePoint(Session.CameraPosition) + (Session.GravityDirection * 20), Session.GravityAxisA, -(Vector3D)Session.GravityDirection);
                }

                bool previousUnderwater = Session.CameraUnderwater;

                Session.CameraDepth = WaterMod.Session.ClosestWater.GetDepth(ref Session.CameraPosition);
                Session.CameraHottub = WaterUtils.HotTubUnderwater(ref Session.CameraPosition);
                Session.CameraUnderwater = Session.CameraDepth <= 0 || Session.CameraHottub;

                if (previousUnderwater != Session.CameraUnderwater)
                {
                    if (Session.CameraUnderwater)
                    {
                        //Camera enters water
                        lock (effectLock)
                        {
                            foreach (var fish in Fishes)
                            {
                                if (fish == null || fish.Billboard == null)
                                    continue;

                                if (!fish.InScene)
                                {
                                    MyTransparentGeometry.AddBillboard(fish.Billboard, true);
                                    fish.InScene = true;
                                }
                            }

                            foreach (var bird in Seagulls)
                            {
                                if (bird == null || bird.Billboard == null)
                                    continue;

                                if (bird.InScene)
                                {
                                    MyTransparentGeometry.RemovePersistentBillboard(bird.Billboard);
                                    bird.InScene = false;
                                }
                            }

                            foreach (var bubble in AmbientBubbles)
                            {
                                if (bubble == null || bubble.Billboard == null)
                                    continue;

                                if (!bubble.InScene)
                                {
                                    MyTransparentGeometry.AddBillboard(bubble.Billboard, true);
                                    bubble.InScene = true;
                                }
                            }

                            foreach (var splash in SimulatedSplashes)
                            {
                                if (splash == null || splash.Billboard == null)
                                    continue;

                                if (splash.InScene)
                                {
                                    MyTransparentGeometry.RemovePersistentBillboard(splash.Billboard);
                                    splash.InScene = false;
                                }
                            }
                        }
                    }
                    else
                    {
                        //Camera exits water
                        lock (effectLock)
                        {
                            foreach (var fish in Fishes)
                            {
                                if (fish == null || fish.Billboard == null)
                                    continue;

                                if (fish.InScene)
                                {
                                    MyTransparentGeometry.RemovePersistentBillboard(fish.Billboard);
                                    fish.InScene = false;
                                }
                            }

                            foreach (var splash in SimulatedSplashes)
                            {
                                if (splash == null || splash.Billboard == null)
                                    continue;

                                if (!splash.InScene)
                                {
                                    MyTransparentGeometry.AddBillboard(splash.Billboard, true);
                                    splash.InScene = true;
                                }
                            }

                            foreach (var bird in Seagulls)
                            {
                                if (bird == null || bird.Billboard == null)
                                    continue;

                                if (!bird.InScene)
                                {
                                    MyTransparentGeometry.AddBillboard(bird.Billboard, true);
                                    bird.InScene = true;
                                }
                            }

                            foreach (var bubble in AmbientBubbles)
                            {
                                if (bubble == null || bubble.Billboard == null)
                                    continue;

                                if (bubble.InScene)
                                {
                                    MyTransparentGeometry.RemovePersistentBillboard(bubble.Billboard);
                                    bubble.InScene = false;
                                }
                            }
                        }
                    }
                }

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
                    weatherEffects.FogDensityOverride = (float)(0.06 + (0.00125 * (1.0 + (-Session.CameraDepth / 100))));
                else
                    weatherEffects.FogDensityOverride = 0.001f;

                //TODO
                /*if (Session.CameraAirtight)
                    weatherEffects.SunSpecularColorOverride = null;
                else
                    weatherEffects.SunSpecularColorOverride = WaterMod.Session.ClosestWater.FogColor;*/

                weatherEffects.FogColorOverride = Vector3.Lerp(WaterMod.Session.ClosestWater.FogColor, Vector3.Zero, (float)Math.Min(-Math.Min(Session.CameraDepth + 200, 0) / 800, 1)) * Math.Min(nightValue + 0.3f, 1f);
                weatherEffects.SunIntensityOverride = Math.Max(MathHelper.Lerp(100, 0, (float)Math.Min(-Session.CameraDepth / (800 / (WaterMod.Session.ClosestWater.Material.Density / 1000)), 1)) * Math.Min(nightValue + 0.3f, 1f), 0);
            }

            //Only Change fog once above water and all the time underwater
            if (previousUnderwaterState != Session.CameraUnderwater)
            {
                previousUnderwaterState = Session.CameraUnderwater;

                if (Session.CameraUnderwater)
                {
                    weatherEffects.ParticleVelocityOverride = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                    //TODO weatherEffects.SunColorOverride = Vector3.Lerp(Vector3.One, WaterMod.Session.ClosestWater.FogColor, 0.5f);
                    weatherEffects.FogMultiplierOverride = 1;
                    weatherEffects.FogAtmoOverride = 1;
                    weatherEffects.FogSkyboxOverride = 1;
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
                    /*weatherEffects.SunColorOverride = null; TODO
                    weatherEffects.SunSpecularColorOverride = null;*/
                }
            }

            foreach (var Billboard in QuadBillboards)
            {
                MyQuadD Quad = Billboard.Quad;
                //MyTransparentGeometry.AddQuad(Billboard.Material, ref Quad,  Vector4.One, ref zeroVector);
                MyTransparentGeometry.AddQuad(Billboard.Material, ref Quad, Billboard.Color, ref zeroVector);
            }

            MyTransparentGeometry.AddBillboards(BillboardCache, false);

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
            if (WaterMod.Session.ClosestPlanet?.HasAtmosphere == true && WaterMod.Session.ClosestWater != null)
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
                    //TODO USE DYNAMIC CRUSH DEPTH
                    if (Session.CameraDepth < -WaterMod.Session.ClosestWater.CrushDamage)
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

                            //TODO CRUSH DAMAGE STUF
                            if (MyAPIGateway.Session.Player?.Character != null)
                                if (Session.CameraUnderwater && !AmbientBoatSoundEmitter.IsPlaying && Session.InsideGrid > 10 && Session.CameraDepth < -WaterMod.Session.ClosestWater.CrushDamage)
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

                        //EnvironmentBeachSoundEmitter.SetPosition(WaterMod.Session.ClosestPlanet.GetClosestSurfacePointGlobal(cameraPosition));

                        if (!EnvironmentBeachSoundEmitter.IsPlaying)
                            EnvironmentBeachSoundEmitter.PlaySound(WaterData.EnvironmentBeachSound, force2D: true);

                        if (closestWeather == null && Seagulls != null && WaterMod.Session.ClosestWater.EnableSeagulls && nightValue > 0.5f)
                            foreach (var seagull in Seagulls)
                            {
                                if (seagull == null)
                                    continue;

                                if (MyUtils.GetRandomInt(0, 300) < 1)
                                {
                                    seagull.Caw();
                                }
                            }

                        if (EnvironmentUnderwaterSoundEmitter.IsPlaying)
                            EnvironmentUnderwaterSoundEmitter.StopSound(true, false);
                    }

                    float volumeMultiplier = (25f - Math.Max(Math.Min(Session.InsideGrid + Session.InsideVoxel, 25) - 5, 0)) / 25f * Settings.Volume;
                    EnvironmentUnderwaterSoundEmitter.VolumeMultiplier = volumeMultiplier;
                    EnvironmentOceanSoundEmitter.VolumeMultiplier = (float)(volumeMultiplier * MathHelperD.Clamp((500f - Session.CameraDepth) / 500.0, 0, 1.0) * ((25.0 - Session.InsideVoxel) / 25.0));
                    EnvironmentBeachSoundEmitter.VolumeMultiplier = (float)(volumeMultiplier * MathHelperD.Clamp((100f - Session.CameraDepth) / 100.0, 0, 1.0) * ((25.0 - Session.InsideVoxel) / 25.0));
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
            if (WaterMod.Session.ClosestWater == null || WaterMod.Session.ClosestPlanet == null)
                return;

            lock (effectLock)
            {
                if (SurfaceSplashes != null && SurfaceSplashes.Count > 0)
                    for (int i = SurfaceSplashes.Count - 1; i >= 0; i--)
                    {
                        Splash splash = SurfaceSplashes[i];
                        if (splash == null || splash.life > splash.maxLife)
                        {
                            SurfaceSplashes.RemoveAtFast(i);
                            continue;
                        }
                        splash.life++; //todo redo
                    }

                if (SimulatedSplashes != null && SimulatedSplashes.Count > 0)
                    for (int i = SimulatedSplashes.Count - 1; i >= 0; i--)
                    {
                        SimulatedSplash Splash = SimulatedSplashes[i];
                        if (Splash == null || Splash.MarkedForClose)
                        {
                            if (Splash != null && Splash.InScene)
                                MyTransparentGeometry.RemovePersistentBillboard(Splash.Billboard);

                            SimulatedSplashes.RemoveAtFast(i);
                            continue;
                        }
                        else
                        {
                            Splash.Simulate();
                        }
                    }
                if (Session.ClosestWater.Material.DrawBubbles)
                {
                    if (Bubbles != null && Session.ClosestWater.Material.DrawBubbles && Bubbles.Count > 0)
                        for (int i = Bubbles.Count - 1; i >= 0; i--)
                        {
                            AnimatedPointBillboard Bubble = Bubbles[i];
                            if (Bubble == null || Bubble.MarkedForClose)
                            {
                                if (Bubble != null && Bubble.InScene)
                                    MyTransparentGeometry.RemovePersistentBillboard(Bubble.Billboard);

                                Bubbles.RemoveAtFast(i);
                                continue;
                            }
                            else
                            {
                                Bubble.Simulate();
                            }
                        }
                    if (AmbientBubbles != null && Session.CameraUnderwater && Session.ClosestWater.Material.DrawBubbles)
                        for (int i = 0; i < AmbientBubbles.Length; i++)
                        {
                            AnimatedPointBillboard Bubble = AmbientBubbles[i];
                            if (Bubble == null || Bubble.MarkedForClose)
                            {
                                if (Bubble != null && Bubble.InScene)
                                    MyTransparentGeometry.RemovePersistentBillboard(Bubble.Billboard);

                                Vector3D randomPosition = Session.CameraPosition + (MyUtils.GetRandomVector3Normalized() * MyUtils.GetRandomFloat(5, 40));
                                IMyCubeGrid grid = WaterUtils.GetApproximateGrid(Session.CameraPosition);

                                if (grid != null)
                                {
                                    for (int j = 0; j < 10; j++)
                                    {
                                        randomPosition = Session.CameraPosition + (MyUtils.GetRandomVector3Normalized() * MyUtils.GetRandomFloat(5, 40));
                                        Vector3I localPosition = grid.WorldToGridInteger(randomPosition);
                                        if (grid.GetCubeBlock(localPosition) == null && !grid.IsRoomAtPositionAirtight(localPosition))
                                        {
                                            AmbientBubbles[i] = new AnimatedPointBillboard(Session.CameraPosition + (MyUtils.GetRandomVector3Normalized() * MyUtils.GetRandomFloat(5, 40)), -WaterMod.Session.GravityDirection, 0.05f, MyUtils.GetRandomInt(150, 250), 0, WaterData.BubbleMaterial);
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    AmbientBubbles[i] = new AnimatedPointBillboard(Session.CameraPosition + (MyUtils.GetRandomVector3Normalized() * MyUtils.GetRandomFloat(5, 40)), -WaterMod.Session.GravityDirection, 0.05f, MyUtils.GetRandomInt(150, 250), 0, WaterData.BubbleMaterial);
                                }
                                
                            }
                            else
                            {
                                Bubble.Simulate();
                            }
                        }
                }

                if (WaterMod.Session.ClosestWater.EnableFish && WaterMod.Session.CameraUnderwater)
                    for (int i = 0; i < Fishes.Length; i++)
                    {
                        Fish Fish = Fishes[i];
                        if (Fish == null || Fish.MarkedForClose)
                        {
                            Vector3D NewPosition = WaterMod.Session.CameraClosestWaterPosition + (MyUtils.GetRandomPerpendicularVector(WaterMod.Session.GravityDirection) * MyUtils.GetRandomFloat(WaterMod.Session.ClosestWater.WaveHeight, 100)) + (WaterMod.Session.GravityDirection * (Session.ClosestWater.WaveHeight + Session.ClosestWater.TideHeight + MyUtils.GetRandomFloat(1, (float)(Session.CameraAltitude - Session.CameraDepth))));

                            Fishes[i] = new Fish(NewPosition, MyUtils.GetRandomPerpendicularVector(WaterMod.Session.GravityDirection), WaterMod.Session.GravityDirection, MyUtils.GetRandomInt(1000, 2000), 1);
                        }
                        else
                        {
                            Fish.Simulate();
                        }
                    }

                if (WaterMod.Session.ClosestWater.EnableSeagulls && !WaterMod.Session.CameraUnderwater)
                    for (int i = 0; i < Seagulls.Length; i++)
                    {
                        Seagull Seagull = Seagulls[i];
                        if (Seagull == null || Seagull.MarkedForClose)
                        {
                            Vector3D NewPosition = WaterMod.Session.CameraClosestPosition - (WaterMod.Session.GravityDirection * (WaterMod.Session.CameraAltitude + WaterMod.Session.CameraDepth + MyUtils.GetRandomDouble(10, 50))) + (MyUtils.GetRandomPerpendicularVector(WaterMod.Session.GravityDirection) * MyUtils.GetRandomFloat(0, 100));

                            Seagulls[i] = new Seagull(NewPosition, MyUtils.GetRandomPerpendicularVector(WaterMod.Session.GravityDirection), WaterMod.Session.GravityDirection, MyUtils.GetRandomInt(2000, 3000), 1);
                        }
                        else
                        {
                            Seagull.Simulate();
                        }
                    }
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

        public void SyncToServer(bool rebuildLOD)
        {
            if (MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
            {
                MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(WaterMod.Static.Waters)));

                if (rebuildLOD)
                    RebuildLOD();
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
}