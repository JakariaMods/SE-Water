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

        List<Splash> Splashes = new List<Splash>(128);
        Seagull[] Seagulls = new Seagull[1];
        Fish[] Fishes = new Fish[1];
        List<Bubble> Bubbles = new List<Bubble>(512);
        SmallBubble[] SmallBubbles = new SmallBubble[1];
        List<Wake> Wakes = new List<Wake>(512);
        List<COBIndicator> indicators = new List<COBIndicator>();

        //Draygo
        ConcurrentDictionary<IMyEntity, DragClientAPI.DragObject> dragAPIGetter = new ConcurrentDictionary<IMyEntity, DragClientAPI.DragObject>();
        DragClientAPI DragAPI = new DragClientAPI();

        Object effectLock = new Object();

        public static class Settings
        {
            public static float Quality = 0.5f;
            public static bool ShowHud = true;
            public static bool ShowCenterOfBuoyancy = false;
            public static bool ShowDepth = true;
            public static bool ShowFog = true;
            public static bool ShowDebug = false;
            public static float Volume = 1f;
        }

        public static class Session
        {
            public static bool CameraAboveWater = false;
            public static float CameraDepth = 0;
            public static bool CameraAirtight = false;
            public static bool CameraUnderwater = false;
            public static int InsideGrid = 0;
            public static int InsideVoxel = 0;
            public static Vector3 CameraPosition = Vector3.Zero;
            public static MatrixD CameraMatrix;
            public static Vector3 CameraRotation = Vector3.Zero;
            public static Vector3 SunDirection = Vector3.Zero;
            public static float SessionTimer = 0;
            public static Vector3D GravityDirection = Vector3.Zero;
            public static Vector3D GravityAxisA = Vector3D.Zero;
            public static Vector3D GravityAxisB = Vector3D.Zero;
            public static Vector3D LastLODBuildPosition = Vector3D.MaxValue;
            public static float DistanceToHorizon = 0;
        }

        //Client variables
        //Vector3[] meshVertices = new Vector3[10 * 10];

        MyPlanet closestPlanet = null;
        Water closestWater = null;
        Vector3D closestFace;
        MyObjectBuilder_WeatherEffect closestWeather = null;
        bool previousUnderwaterState = true;
        List<MyEntity> nearbyEntities = new List<MyEntity>();
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
        public float ambientTimer = 0;
        public float ambientBoatTimer = 0;

        public MyEntity3DSoundEmitter EnvironmentUnderwaterSoundEmitter = new MyEntity3DSoundEmitter(null);
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
            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.WaterModVersion.Replace("{0}", WaterData.Version));

            MyAPIGateway.Multiplayer.RegisterMessageHandler(WaterData.ClientHandlerID, ClientHandler);
            MyAPIGateway.Utilities.MessageEntered += Utilities_MessageEntered;

            MyLog.Default.WriteLine(WaterLocalization.CurrentLanguage.WaterModVersion.Replace("{0}", WaterData.Version));

            weatherEffects = MyAPIGateway.Session.WeatherEffects;
            WaterData.UpdateFovFrustum();

            //Client
            if (!MyAPIGateway.Utilities.IsDedicated)
            {
                TextAPI = new HudAPIv2(TextAPIRegisteredCallback);
                sunOccluder = CreateSunOccluderEntity();
                RecreateWater();
            }

            //Server
            if (MyAPIGateway.Session.IsServer)
            {
                //MyVisualScriptLogicProvider.RespawnShipSpawned += RespawnShipSpawned;
                MyVisualScriptLogicProvider.PrefabSpawnedDetailed += PrefabSpawnedDetailed;
                MyEntities.OnEntityAdd += Entities_OnEntityAdd;
                MyVisualScriptLogicProvider.PlayerConnected += PlayerConnected;
                MyVisualScriptLogicProvider.PlayerDisconnected += PlayerDisconnected;
            }


            //particleOccluder = CreateParticleOccluderEntity();

            initialized = true;

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
            if (prefabName == null)
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
            }
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
                            Water water = new Water(planet, settings);

                            Waters[planet.EntityId] = water;
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
                        Settings.ShowDepth = true;
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
                                MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(Waters)));
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
                                MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(Waters)));
                                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.ToggleFish);
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
                                    MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(Waters.Values));
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
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.GetViscosity.Replace("{0}", closestWater.viscosity.ToString()));
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
                            viscosity = MathHelper.Clamp(viscosity, 0f, 1f);
                            if (closestPlanet == null)
                            {
                                WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanet);
                                return;
                            }

                            foreach (var water in Waters.Values)
                            {
                                if (water.planetID == closestPlanet.EntityId)
                                {
                                    water.viscosity = viscosity;
                                    MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(Waters)));
                                    WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetViscosity.Replace("{0}", water.viscosity.ToString()));
                                    return;
                                }
                            }

                            //If foreach loop doesn't find the planet, assume it doesn't exist
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.NoPlanetWater);
                        }
                        else
                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetViscosityNoParse.Replace("{0}", args[1]));
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
                            radius = MathHelper.Clamp(radius, 0.01f, 2f);

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
                                    MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(Waters)));
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
                                    MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(Waters)));
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

                            MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(Waters)));
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
                                    MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(Waters)));
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
                                    MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(Waters)));
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
                            MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(Waters)));
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
                        MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(Waters)));
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
                        MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(Waters)));
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
                                MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(Waters)));
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
                                MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(Waters)));
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
                                MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(Waters)));
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

                                    MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(Waters)));
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
                                    MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(Waters)));
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
                                    MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(Waters)));
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
                                            MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(Waters)));
                                            WaterUtils.ShowMessage(WaterLocalization.CurrentLanguage.SetCollectRate.Replace("{0}", water.collectionRate.ToString()));
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
            //Draygo
            DragAPI.Close();

            MyAPIGateway.Multiplayer.UnregisterMessageHandler(WaterData.ClientHandlerID, ClientHandler);
            MyAPIGateway.Utilities.MessageEntered -= Utilities_MessageEntered;

            if (MyAPIGateway.Session.IsServer)
            {
                MyVisualScriptLogicProvider.PlayerConnected -= PlayerConnected;
                MyVisualScriptLogicProvider.PlayerConnected -= PlayerDisconnected;
                MyEntities.OnEntityAdd -= Entities_OnEntityAdd;
                //MyVisualScriptLogicProvider.RespawnShipSpawned -= RespawnShipSpawned;
                MyVisualScriptLogicProvider.PrefabSpawnedDetailed -= PrefabSpawnedDetailed;
            }

            EnvironmentUnderwaterSoundEmitter.Cleanup();
            EnvironmentOceanSoundEmitter.Cleanup();
            EnvironmentBeachSoundEmitter.Cleanup();
            AmbientSoundEmitter.Cleanup();
            AmbientBoatSoundEmitter.Cleanup();
        }

        /// <summary>
        /// Called once every tick after the simulation has run
        /// </summary>
        public override void UpdateAfterSimulation()
        {
            tickTimerSecond++;
            if (tickTimerSecond > 60)
            {
                UpdateAfterSecond();
                tickTimerSecond = 0;
            }

            tickTimer++;
            //Delayed updating
            if (tickTimer > tickTimerMax)
            {
                tickTimer = 0;

                if (Waters == null)
                    return;

                //Server and Client

                foreach (var Key in Waters.Keys)
                {
                    if (!MyEntities.EntityExists(Key))
                    {
                        Waters.Remove(Key);
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
                        BoundingSphereD sphere = new BoundingSphereD(water.position, water.currentRadius + water.waveHeight);
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
                        nightValue = 1f - MyMath.Clamp(Vector3.Dot(Session.GravityDirection, WaterMod.Session.SunDirection) + 0.05f, 0.05f, 1f);

                    if (closestWater != null && closestPlanet != null)
                    {
                        if (TextAPI.Heartbeat && DepthMeter.Visible)
                        {
                            string message;
                            if (Session.CameraDepth < -closestWater.waveHeight * 2)
                                message = WaterLocalization.CurrentLanguage.Depth.Replace("{0}", Math.Round(-closestWater.GetDepthSimple(Session.CameraPosition)).ToString());
                            else
                                message = WaterLocalization.CurrentLanguage.Depth.Replace("{0}", Math.Round(-Session.CameraDepth).ToString());

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

                        if (closestWeather != null && (closestWeather.Weather.StartsWith("Rain") || closestWeather.Weather.StartsWith("ThunderStorm")))
                        {
                            if (weatherEffects.GetWeatherIntensity(Session.CameraPosition, closestWeather) > 0.1f)
                                for (int i = 0; i < (int)(Settings.Quality * 20); i++)
                                {
                                    Splashes.Add(new Splash(closestWater.GetClosestSurfacePoint(Session.CameraPosition + (MyUtils.GetRandomVector3Normalized() * 50)), 0.5f, false));
                                }
                        }

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
                                    ListReader<MyCubeBlock> fatBlocks;
                                    if (FatBlockStorage.Storage.TryGetValue(entity.EntityId, out fatBlocks))
                                    {
                                        foreach (var block in fatBlocks)
                                        {
                                            if (block == null)
                                                continue;

                                            bool belowWater = closestWater.IsUnderwater(block.PositionComp.GetPosition());
                                            if (!block.IsFunctional)
                                            {
                                                if (block.IsBuilt && belowWater)
                                                {
                                                    block.StopDamageEffect(true);
                                                    if (MyUtils.GetRandomInt(0, 100) < 7)
                                                        lock (effectLock)
                                                            Bubbles.Add(new Bubble(block.PositionComp.GetPosition(), block.CubeGrid.GridSizeHalf));
                                                }
                                                continue;
                                            }

                                            if (belowWater)
                                            {
                                                if (block is IMyGasTank)
                                                {
                                                    IMyGasTank gasTank = block as IMyGasTank;
                                                    capacity = (float)gasTank.FilledRatio * gasTank.Capacity;
                                                    litres += capacity;
                                                    center += block.Position * capacity;
                                                    continue;
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

                                        if (depth < 1.3f && depth > -5)
                                        {
                                            if (horizontalVelocityLength > 1f) //Horizontal Velocity
                                            {
                                                float MinRadius = ((horizontalVelocityLength * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS) * 2.5f);
                                                float MaxExtents = Math.Max(entity.PositionComp.LocalVolume.GetBoundingBox().Extents.Length(), MinRadius);

                                                Wakes.Add(new Wake(closestWater.GetClosestSurfacePoint(centerOfBuoyancy, -0.5f),
                                                    horizontalVelocity * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS_HALF,
                                                    Session.GravityDirection,
                                                    new Vector4(Math.Min(horizontalVelocityLength, 5f) / 5f, Math.Min(horizontalVelocityLength, 5f) / 5f, Math.Min(horizontalVelocityLength, 5f) / 5f, Math.Min(horizontalVelocityLength, 5f) / 5f),
                                                    MaxExtents / 4,
                                                   MaxExtents / 4));
                                            }

                                            if (verticalVelocity.Length() > 2f) //Vertical Velocity
                                            {
                                                if (entity is MyCubeGrid)
                                                    Splashes.Add(new Splash(closestWater.GetClosestSurfacePoint(centerOfBuoyancy), entity.PositionComp.LocalVolume.Radius));
                                                else
                                                    Splashes.Add(new Splash(closestWater.GetClosestSurfacePoint(centerOfBuoyancy), entity.Physics.Speed));
                                            }

                                        }
                                    }
                                    continue;
                                }

                                if (closestWater.IsUnderwater(entity.PositionComp.GetPosition()))
                                {
                                    if (entity is MyFloatingObject)
                                    {
                                        if (MyUtils.GetRandomFloat(0, 1) < 0.12f && !WaterUtils.IsPositionAirtight(entity.PositionComp.GetPosition()))
                                            CreateBubble(entity.PositionComp.GetPosition(), 0.4f);

                                        continue;
                                    }

                                    if (entity is IMyCharacter)
                                    {
                                        Vector3D position = (entity as IMyCharacter).GetHeadMatrix(false, false, false, false).Translation;

                                        float Depth = closestWater.GetDepth(position);
                                        if (Depth < 1 && Depth > -1 && verticalVelocity.Length() > 2f)
                                        {
                                            Splashes.Add(new Splash(closestWater.GetClosestSurfacePoint(position), entity.Physics.Speed));
                                        }

                                        if (Depth < 0 && MyUtils.GetRandomFloat(0, 1) < 0.12f && !WaterUtils.IsPositionAirtight(position))
                                            CreateBubble(position, 0.2f);

                                        continue;
                                    }
                                }
                            }
                    }
                }
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
                }

                if (closestWater != null)
                    MyAPIGateway.Parallel.StartBackground(SimulateEffects);
            }
        }

        /// <summary>
        /// Called once every second (60 ticks) after the simulation has run
        /// </summary>
        private void UpdateAfterSecond()
        {
            try
            {
                Session.SunDirection = MyVisualScriptLogicProvider.GetSunDirection();

                if (Settings.ShowDebug && MyAPIGateway.Session?.Player?.Character != null)
                {
                    MyAPIGateway.Utilities.ShowNotification("" + WaterData.DotMaxFOV, 1000);
                }

                players.Clear();
                MyAPIGateway.Players.GetPlayers(players);

                //Depricated
                //MyAPIGateway.Utilities.SendModMessage(WaterModAPI.ModHandlerID, MyAPIGateway.Utilities.SerializeToBinary(waters));

                //Clients
                if (!MyAPIGateway.Utilities.IsDedicated)
                {
                    if (Session.CameraDepth > -100)
                        foreach (var water in Waters.Values)
                        {
                            foreach (var face in water.waterFaces)
                            {
                                face.ConstructTree();
                            }
                        }

                    Session.CameraAboveWater = (closestPlanet != null && closestWater != null && !Session.CameraUnderwater) ? closestWater.IsUnderwater(closestPlanet.GetClosestSurfacePointGlobal(Session.CameraPosition)) : false;
                }

                //Servers
                if (MyAPIGateway.Session.IsServer)
                {
                    //Player Damage
                    if (players != null)
                        foreach (var water in Waters.Values)
                        {
                            foreach (var player in players)
                            {
                                if (player?.Character == null || ((player.Controller?.ControlledEntity?.Entity as MyCockpit) != null && (player.Controller.ControlledEntity.Entity as MyCockpit).OxygenFillLevel > 0f) || !(player.Controller.ControlledEntity is IMyCharacter))
                                    continue;

                                float depth = water.GetDepth(player.Character.GetHeadMatrix(false).Translation);

                                if (!MyAPIGateway.Session.SessionSettings.EnableOxygen || !MyAPIGateway.Session.SessionSettings.EnableOxygenPressurization)
                                    continue;

                                MyCharacterOxygenComponent oxygenComponent = player.Character.Components.Get<MyCharacterOxygenComponent>();

                                if (oxygenComponent != null && depth < 0 && !WaterUtils.IsPositionAirtight(player.Character.GetHeadMatrix(false).Translation))
                                {
                                    if (oxygenComponent.HelmetEnabled)
                                    {
                                        //Increase oxygen usage to prevent infinite oxygen
                                        //oxygenComponent.SuitOxygenAmount -= 5f;
                                    }
                                    else
                                    {
                                        //Suffocate
                                        if (MyAPIGateway.Session.SessionSettings.EnableOxygen)
                                            player.Character.DoDamage(3f, MyDamageType.Asphyxia, true);
                                    }

                                    //Pressure Crushing
                                    if (depth <= -water.crushDepth)
                                    {
                                        player.Character.DoDamage(1f + 1f * (-(depth + water.crushDepth) / 300), MyDamageType.Temperature, true);
                                    }
                                }
                            }
                        }
                }
            }
            catch (Exception e)
            {
                //Keen crash REEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEE
                MyLog.Default.WriteLine(e);
                //WaterUtils.SendChatMessage(e.ToString());
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
            Splashes.Add(new Splash(Position, Radius, Audible));
        }

        /// <summary>
        /// Simulates water physics
        /// </summary>
        public void SimulatePhysics()
        {
            if (MyAPIGateway.Session.IsServer)
            {
                foreach (var water in Waters.Values)
                {
                    if (water.viscosity == 0 && water.buoyancy == 0)
                        continue;

                    water.waveTimer += water.waveSpeed;
                    //water.currentRadius = (float)Math.Max(water.radius + (Math.Sin(water.waveTimer) * water.waveHeight), 0);
                    water.currentRadius = water.radius;

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

                    if (water.playerDrag)
                    {
                        //Character Drag
                        if (players != null)
                            foreach (var player in players)
                            {
                                if (player?.Character?.Physics == null || player.Character.Physics.Speed < 0.1f)
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
                    closestWater.waveTimer += closestWater.waveSpeed;
                    //closestWater.currentRadius = (float)Math.Max(closestWater.radius + (Math.Sin(closestWater.waveTimer) * closestWater.waveHeight), 0);
                    closestWater.currentRadius = closestWater.radius;

                    if (MyAPIGateway.Session.Player?.Character != null)
                        SimulatePlayer(closestWater, MyAPIGateway.Session.Player.Character);

                    if (MyAPIGateway.Session.ControlledObject?.Entity is MyCockpit)
                    {
                        SimulateGrid(closestWater, (MyAPIGateway.Session.ControlledObject as MyCockpit).CubeGrid);
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
        private MyEntity CreateSunOccluderEntity()
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
            ent.Render.Transparency = 1f;
            ent.PositionComp.SetPosition(Vector3D.MaxValue);
            ent.Render.DrawInAllCascades = true;
            ent.Render.UpdateTransparency();
            ent.InScene = true;
            ent.Render.UpdateRenderObject(true, false);

            return ent;
        }

        /// <summary>
        /// Creates mesh to block particles below water
        /// </summary>
        private MyEntity CreateParticleOccluderEntity()
        {
            var ent = new MyEntity();
            ent.Init(null, ModContext.ModPath + @"\Models\Water.mwm", null, null, null);
            ent.Render.CastShadows = false;
            ent.Render.DrawOutsideViewDistance = true;
            ent.IsPreview = true;
            ent.Save = false;
            ent.SyncFlag = false;
            ent.NeedsWorldMatrix = false;
            ent.Flags |= EntityFlags.IsNotGamePrunningStructureObject;
            MyEntities.Add(ent, false);
            ent.Render.Transparency = 0f;
            ent.PositionComp.SetPosition(Vector3D.MaxValue);
            ent.PositionComp.Scale = 250;
            ent.Render.UpdateTransparency();
            ent.InScene = true;
            ent.Render.UpdateRenderObject(true, false);

            return ent;
        }

        public void SimulatePlayer(Water water, IMyCharacter player)
        {
            Vector3D position = player.GetHeadMatrix(false, false).Translation;
            if (water.IsUnderwater(position) && !WaterUtils.IsPositionAirtight(position))
            {
                Vector3 characterforce = -player.Physics.LinearVelocity * water.viscosity * player.Physics.Mass * 12;

                if ((!player.EnabledThrusts || !player.EnabledDamping) && WaterUtils.IsPlayerStateFloating(player.CurrentMovementState))
                    characterforce += player.Physics.Mass * -player.Physics.Gravity * 1.2f * Math.Min(water.buoyancy, 1f);

                if (characterforce.IsValid())
                    player.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, characterforce, null, null);
            }
        }

        public void SimulateGrid(Water water, MyCubeGrid grid)
        {
            if (grid == null || grid.Physics == null || grid.Physics.IsStatic)
                return;

            Vector3D gridCenter = grid.PositionComp.WorldVolume.Center;
            Vector3 linearVelocity = grid.Physics.LinearVelocity;
            float gridRadius = grid.PositionComp.LocalVolume.Radius;

            Vector3D center = Vector3.Zero;
            double litres = 0;
            float capacity = 0;
            float depth = water.GetDepth(gridCenter);
            float tankHealth = 0;
            float totalTanks = 0;

            float WaterDist = depth + gridRadius / 2f;
            float percentGridUnderWater = 1f - MathHelper.Clamp(WaterDist / gridRadius, 0f, 1f);

            Vector3 dragForce = -(linearVelocity * grid.Physics.Mass * water.viscosity * (1 + (Math.Min(-water.GetDepth(gridCenter), 0) / 2000f))) * linearVelocity.Length() * percentGridUnderWater;

            //Drag
            float newAngularVelocity = 1f - (percentGridUnderWater * water.viscosity * (grid.Physics.Speed / 20f));

            if (DragAPI.Heartbeat)
            {
                var api = dragAPIGetter.GetOrAdd(grid, DragClientAPI.DragObject.Factory);

                if (api != null)
                {
                    //should increase the viscositymultiplier so its 5x more drag than air. Let the drag mod run the drag calculation. 
                    //api.ViscosityMultiplier = 1000;
                    api.ViscosityMultiplier = 1f + (percentGridUnderWater * water.viscosity * 1000f * (1f + (Math.Max(WaterDist, 0) / 2000f)));
                }

                if (dragForce.IsValid())
                    grid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, dragForce / 4, null, null, null, false, false);

                if (newAngularVelocity != 0)
                    grid.Physics.AngularVelocity *= newAngularVelocity;

            } //End Draygo
            else
            {
                if (newAngularVelocity != 0)
                    grid.Physics.AngularVelocity *= newAngularVelocity;

                if (dragForce.IsValid())
                    grid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, dragForce / 2, null, null, null, false, false);
            }
            //End Drag
            //ListReader<MyCubeBlock> fatBlocks;

            ListReader<MyCubeBlock> fatBlocks;
            if (FatBlockStorage.Storage.TryGetValue(grid.EntityId, out fatBlocks))
            {
                foreach (var block in fatBlocks)
                {
                    Vector3D worldPosition = block.PositionComp.GetPosition();

                    if (block is IMyGasTank)
                    {
                        totalTanks++;
                        tankHealth += 1 - (block.SlimBlock as IMySlimBlock).CurrentDamage / (block.SlimBlock as IMySlimBlock).MaxIntegrity;

                        if (!block.IsFunctional)
                            continue;

                        float depth2 = percentGridUnderWater > 0.9f ? MathHelper.Min(-water.GetDepth(worldPosition), 1) : MathHelper.Min(-water.GetDepth(worldPosition), 1);

                        if (depth2 > 0f)
                        {
                            IMyGasTank gasTank = block as IMyGasTank;
                            capacity = (float)gasTank.FilledRatio * gasTank.Capacity * depth2;
                            litres += capacity;
                            center += block.Position * capacity;
                        }
                        continue;
                    }

                    if (block is IMyWheel)
                    {
                        totalTanks++;
                        tankHealth++;

                        if (!block.IsFunctional)
                            continue;

                        float depth2 = MathHelper.Min((water.currentRadius + 0.5f) - Vector3.Distance(water.position, worldPosition), 1);

                        if (depth2 > 0f)
                        {
                            if (grid.GridSizeEnum == MyCubeSize.Large)
                                capacity = block.BlockDefinition.Mass * 31;
                            else
                                capacity = block.BlockDefinition.Mass * 25;

                            litres += capacity;
                            center += block.Position * capacity;
                        }
                        continue;
                    }
                }

                if (litres != 0)
                {
                    Vector3D centerOfBuoyancy = grid.GridIntegerToWorld(center / litres);

                    double force = 0;
                    if (grid.GridSizeEnum == MyCubeSize.Large)
                        force = litres * (1 + (-depth / 5000f)) / 50f * (Math.Pow(tankHealth / totalTanks, 2) * water.buoyancy);
                    else
                        force = litres * (1 + (-depth / 5000f)) / 20f * (Math.Pow(tankHealth / totalTanks, 2) * water.buoyancy);

                    //force -= Math.Abs(Vector3.Dot(grid.Physics.LinearVelocity, Vector3D.Normalize(grid.Physics.Gravity))) * (grid.Physics.Speed * grid.Physics.Mass) * 0.01;
                    force += Vector3.Dot(Vector3D.Normalize(linearVelocity), Vector3D.Normalize(grid.Physics.Gravity)) * (linearVelocity.Length() * grid.Physics.Mass) * 0.05;
                    Vector3 newForce = -Vector3D.Normalize(grid.Physics.Gravity) * force * 60;

                    if (newForce.IsValid())
                        grid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, newForce, centerOfBuoyancy, null, null, true, false);
                }
            }
        }

        /// <summary>
        /// Draw
        /// </summary>
        public override void Draw()
        {
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

            //Text Hud API Update
            if (TextAPI.Heartbeat)
            {
                DepthMeter.Visible = Settings.ShowDepth && Session.CameraDepth < 0;
            }

            if (closestWater != null)
            {
                WaterData.UpdateFovFrustum();

                //if (Session.CameraDepth < 10 && Session.CameraDepth > 0 && MyAPIGateway.Session?.Player != null)
                //MyTransparentGeometry.AddBillboardOriented(WaterData.BlankMaterial, Vector4.One, closestWater.GetClosestSurfacePoint(MyAPIGateway.Session.Player.GetPosition()), gravAxisA, gravAxisB, 10, 0, blendType: BlendTypeEnum.Standard);

                //particleOccluder.Render.Visible = !Session.CameraUnderwater;
                sunOccluder.Render.Visible = Session.CameraUnderwater || (closestWater.Intersects(Session.CameraPosition, Session.CameraPosition + (Session.SunDirection * 1000)) != 0);

                if (sunOccluder.Render.Visible)
                    sunOccluder.WorldMatrix = MatrixD.CreateWorld(Session.CameraPosition + (Session.SunDirection * 1000), -Vector3.CalculatePerpendicularVector(Session.SunDirection), -Session.SunDirection);

                //particleOccluder.WorldMatrix = MatrixD.CreateWorld(closestWater.GetClosestSurfacePoint(Session.CameraPosition), gravAxisA, gravAxisB);

                Session.CameraDepth = closestWater.GetDepth(Session.CameraPosition);
                Session.CameraUnderwater = Session.CameraDepth <= 0;
            }
            else
            {
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
                weatherEffects.FogColorOverride = Vector3D.Lerp(closestWater.fogColor, Vector3D.Zero, -Session.CameraDepth / closestWater.crushDepth);
                weatherEffects.FogAtmoOverride = 1;
                weatherEffects.FogSkyboxOverride = 1;

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
                }
            }

            lock (effectLock)
            {
                float lifeRatio = 0;

                foreach (var bubble in Bubbles)
                {
                    if (bubble == null)
                        continue;

                    MyTransparentGeometry.AddPointBillboard(WaterData.BubblesMaterial, WaterData.BubbleColor * (1f - ((float)bubble.life / (float)bubble.maxLife)), bubble.position, bubble.radius, bubble.angle);
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
                    Vector4 Color = new Vector4(nightValue, nightValue, nightValue, 1);

                    foreach (var splash in Splashes)
                    {
                        if (splash == null)
                            continue;

                        lifeRatio = (float)splash.life / splash.maxLife;
                        MyTransparentGeometry.AddBillboardOriented(WaterData.SplashMaterial, Color * Math.Min(1f - lifeRatio, 1), splash.position, Session.GravityAxisA, Session.GravityAxisB, splash.radius * lifeRatio);
                    }

                    foreach (var wake in Wakes)
                    {
                        if (wake == null)
                            continue;

                        lifeRatio = (float)Math.Sin((float)wake.life / wake.maxLife);
                        float RedGreen = Math.Max(nightValue - 0.25f, 0.05f) * (float)Math.Abs(1f - lifeRatio);
                        MyTransparentGeometry.AddBillboardOriented(WaterData.WakeMaterial, new Vector4(RedGreen, RedGreen, RedGreen, 0.1f) * wake.colorMultiplier, wake.position, wake.leftVector, wake.upVector, (wake.minimumRadius / 2) + wake.maximumRadius * lifeRatio);
                    }

                    if (closestWeather == null && closestWater?.enableSeagulls == true && nightValue > 0.75f && closestWeather == null)
                        foreach (var seagull in Seagulls)
                        {
                            if (seagull == null)
                                continue;

                            //MyTransparentGeometry.AddPointBillboard(seagullMaterial, WaterData.WhiteColor, seagull.position, 1, 0);
                            MyTransparentGeometry.AddBillboardOriented(WaterData.SeagullMaterial, WaterData.WhiteColor, seagull.Position, seagull.LeftVector, seagull.UpVector, 0.7f);
                        }
                }
            }

            if (closestWater != null)
                Session.DistanceToHorizon = Session.CameraUnderwater ? 500 : (float)Math.Sqrt(((Session.CameraDepth + closestWater.radius) * (Session.CameraDepth + closestWater.radius)) - (closestWater.radius * closestWater.radius));

            if (closestWater == null || closestPlanet == null)
                Session.DistanceToHorizon = float.PositiveInfinity;

            if (Session.CameraDepth > -200)
                MyAPIGateway.Parallel.Do(() =>
                {
                    bool rebuildTree = false;
                    if (Vector3D.RectangularDistance(Session.LastLODBuildPosition, Session.CameraPosition) > 15)
                    {
                        rebuildTree = true;
                        Session.LastLODBuildPosition = Session.CameraPosition;
                        WaterData.UpdateFovFrustum();
                    }

                    MyAPIGateway.Parallel.ForEach(Waters.Values, water =>
                    {
                        double waterDiameter = water.radius * 2;
                        double tempTime = Session.SessionTimer * 0.0001;
                        float offset = (float)((tempTime - (int)tempTime) * 0.5);
                        float offset2 = (float)((tempTime - (int)tempTime + 1) * 0.5);

                        if (water?.planet == null)
                            return;

                        if (water.waterFaces == null)
                        {
                            water.waterFaces = new WaterFace[WaterData.Directions.Length];
                            for (int i = 0; i < WaterData.Directions.Length; i++)
                            {
                                water.waterFaces[i] = new WaterFace(water, WaterData.Directions[i]);
                            }
                        }

                        foreach (var face in water.waterFaces)
                        {
                            if (face?.water == null)
                                continue;

                            if (rebuildTree)
                            {
                                face.ConstructTree();
                            }

                            face.Draw(face.water.planetID == closestWater?.planetID);
                        }
                    });
                });
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
                    EnvironmentOceanSoundEmitter.VolumeMultiplier = volumeMultiplier * MyMath.Clamp((100f - Session.CameraDepth) / 100f, 0, 1f);
                    EnvironmentBeachSoundEmitter.VolumeMultiplier = volumeMultiplier * MyMath.Clamp((50f - Session.CameraDepth) / 50f, 0, 1f);
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
            }
        }

        /// <summary>
        /// Simulates bubbles, splashes, wakes, indicators, smallBubbles, seagulls, and fish
        /// </summary>
        public void SimulateEffects()
        {
            float bubbleSpeed = ((MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS / 2) * closestPlanet.Generator.SurfaceGravity);
            lock (effectLock)
            {
                if (Bubbles != null && Bubbles.Count > 0)
                    for (int i = Bubbles.Count - 1; i >= 0; i--)
                    {
                        if (Bubbles[i] == null || Bubbles[i].life > Bubbles[i].maxLife || closestPlanet == null)
                        {
                            Bubbles.RemoveAtFast(i);
                            continue;
                        }
                        Bubbles[i].position += -Session.GravityDirection * bubbleSpeed;
                        Bubbles[i].life++;
                    }
                if (Splashes != null && Splashes.Count > 0)
                    for (int i = Splashes.Count - 1; i >= 0; i--)
                    {
                        if (Splashes[i] == null || Splashes[i].life > Splashes[i].maxLife || closestPlanet == null)
                        {
                            Splashes.RemoveAtFast(i);
                            continue;
                        }
                        Splashes[i].life++;
                    }

                if (Wakes != null && Wakes.Count > 0)
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
                    }

                if (indicators != null && indicators.Count > 0)
                    for (int i = indicators.Count - 1; i >= 0; i--)
                    {
                        if (indicators[i] == null || indicators[i].life > tickTimerMax + 1 || closestPlanet == null)
                        {
                            indicators.RemoveAtFast(i);
                            continue;
                        }
                        indicators[i].life++;
                    }

                if (SmallBubbles != null && Session.CameraUnderwater)
                    for (int i = SmallBubbles.Length - 1; i >= 0; i--)
                    {
                        if (SmallBubbles[i] == null || SmallBubbles[i].life > SmallBubbles[i].maxLife)
                        {
                            SmallBubbles[i] = new SmallBubble(Session.CameraPosition + (MyUtils.GetRandomVector3Normalized() * MyUtils.GetRandomFloat(5, 20)));
                            continue;
                        }
                        SmallBubbles[i].position += -Session.GravityDirection * bubbleSpeed;
                        SmallBubbles[i].life++;
                    }

                if (Seagulls != null && closestWater?.enableSeagulls == true)
                    for (int i = 0; i < Seagulls.Length; i++)
                    {
                        if (Seagulls[i] == null || Seagulls[i].Life > Seagulls[i].MaxLife)
                        {
                            if (Seagulls[i]?.cawSound?.IsPlaying == true)
                                continue;

                            Seagulls[i] = new Seagull(closestWater.GetClosestSurfacePoint(Session.CameraPosition) + (-Session.GravityDirection * MyUtils.GetRandomFloat(25, 150)) + (MyUtils.GetRandomPerpendicularVector(Session.GravityDirection) * MyUtils.GetRandomFloat(-2500, 2500)), MyUtils.GetRandomPerpendicularVector(Session.GravityDirection) * MyUtils.GetRandomFloat(0.05f, 0.15f), -Session.GravityDirection);
                            continue;
                        }

                        Seagulls[i].Life++;
                        Seagulls[i].Position += Seagulls[i].Velocity;
                    }

                if (Fishes != null && closestWater?.enableFish == true)
                    for (int i = 0; i < Fishes.Length; i++)
                    {

                        if (Fishes[i] == null || Fishes[i].Life > Fishes[i].MaxLife)
                        {
                            Vector3D Position = Session.CameraPosition + (MyUtils.GetRandomVector3Normalized() * MyUtils.GetRandomFloat(50, 250));
                            Vector3D Direction = Vector3D.Zero;
                            for (int j = 0; j < 5; j++)
                            {
                                Direction = MyUtils.GetRandomPerpendicularVector(Session.GravityDirection);
                                IHitInfo info;
                                if (!WaterUtils.IsUnderGround(closestPlanet, Position) && !WaterUtils.IsUnderGround(closestPlanet, Position + Direction))
                                    break;
                            }

                            Fishes[i] = new Fish(Position, Direction * MyUtils.GetRandomFloat(0.05f, 0.11f), Session.GravityDirection);
                            continue;
                        }

                        Fishes[i].Life++;
                        Fishes[i].Position += Fishes[i].Velocity;
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

        /// <summary>
        /// Client -> Server and Server -> Client communication
        /// </summary>
        /// <param name="packet"></param>
        private void ClientHandler(byte[] packet)
        {
            Waters = MyAPIGateway.Utilities.SerializeFromBinary<SerializableDictionary<long, Water>>(packet).Dictionary;

            //if (Waters != null)
            //MyAPIGateway.Utilities.SendModMessage(WaterModAPI.ModHandlerID, MyAPIGateway.Utilities.SerializeToBinary(Waters));

            if (!MyAPIGateway.Utilities.IsDedicated)
                RecreateWater();

            if (MyAPIGateway.Session.IsServer)
                SyncClients();
        }
    }
}