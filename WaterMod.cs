﻿using Sandbox.Game;
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
using VRage.Game.Entity.UseObject;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Definitions;
using Draygo.API;
using System.Text;
using SpaceEngineers.Game.Weapons.Guns;
using Sandbox.Game.Weapons;

namespace Jakaria
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    class WaterMod : MySessionComponentBase
    {
        private static Color white = Color.White;

        public List<Water> waters = new List<Water>();
        List<Splash> splashes = new List<Splash>(128);
        Seagull[] seagulls = new Seagull[1];
        Fish[] fishes = new Fish[1];
        List<Bubble> bubbles = new List<Bubble>(512);
        SmallBubble[] smallBubbles = new SmallBubble[1];
        List<Wake> wakes = new List<Wake>(512);
        List<COBIndicator> indicators = new List<COBIndicator>();

        //Draygo
        ConcurrentDictionary<IMyEntity, DragClientAPI.DragObject> dragAPIGetter = new ConcurrentDictionary<IMyEntity, DragClientAPI.DragObject>();
        DragClientAPI DragAPI = new DragClientAPI();
        //End Draygo

        static int waterLayerAmount = 50;
        static float waterLayerSeperation = 0.5f;
        static float waterQuality = 0.5f;
        MyEntity[] waterLayers = new MyEntity[1];

        Object effectLock = new Object();

        //Client variables
        bool showHud = true;
        bool showCenterOfBuoyancy = false;
        bool showDepth = true;
        bool showFog = true;
        MyPlanet closestPlanet = null;
        Water closestWater = null;
        MyObjectBuilder_WeatherEffect closestWeather = null;
        Vector3D gravityDirection = Vector3D.Zero;
        float depth = 0;
        int insideGrid = 0;
        bool underwater = false;
        bool previousUnderwaterState = true;
        List<MyEntity> nearbyEntities = new List<MyEntity>();
        bool initialized = false;
        float nightValue = 0;

        //Text Hud API
        HudAPIv2 TextAPI;
        HudAPIv2.HUDMessage DepthMeter;

        int tickTimer = 0;
        public const int tickTimerMax = 5;

        public MyEntity3DSoundEmitter EnvironmentUnderwaterSoundEmitter = new MyEntity3DSoundEmitter(null);
        public MyEntity3DSoundEmitter EnvironmentOceanSoundEmitter = new MyEntity3DSoundEmitter(null);
        public MyEntity3DSoundEmitter EnvironmentBeachSoundEmitter = new MyEntity3DSoundEmitter(null);
        public MyEntity3DSoundEmitter AmbientSoundEmitter = new MyEntity3DSoundEmitter(null);
        public MyEntity3DSoundEmitter AmbientBoatSoundEmitter = new MyEntity3DSoundEmitter(null);

        public float ambientTimer = 0;
        public float ambientBoatTimer = 0;

        public static WaterMod Static;

        public WaterMod()
        {
            if (Static == null)
                Static = this;
        }

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.WaterModVersion.Replace("{0}", WaterData.Version));

            MyAPIGateway.Multiplayer.RegisterMessageHandler(WaterData.ClientHandlerID, ClientHandler);
            MyAPIGateway.Utilities.MessageEntered += Utilities_MessageEntered;

            MyLog.Default.WriteLine(WaterLocalization.CurrentLanguage.WaterModVersion.Replace("{0}", WaterData.Version));

            if (!MyAPIGateway.Utilities.IsDedicated)
            {
                TextAPI = new HudAPIv2(TextAPIRegisteredCallback);
                RecreateWater();
            }
            if (MyAPIGateway.Session.IsServer)
            {
                //MyVisualScriptLogicProvider.RespawnShipSpawned += RespawnShipSpawned;
                MyVisualScriptLogicProvider.PrefabSpawnedDetailed += PrefabSpawnedDetailed;
                MyAPIGateway.Entities.OnEntityAdd += Entities_OnEntityAdd;
                MyVisualScriptLogicProvider.PlayerConnected += PlayerConnected;
            }

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
            if (WaterData.DropContainerNames.Contains(prefabName))
            {
                MyEntity entity = MyEntities.GetEntityById(entityId);
                MyPlanet planet = MyGamePruningStructure.GetClosestPlanet(entity.PositionComp.GetPosition());
                foreach (var water in waters)
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

        public Water GetClosestWater(Vector3 position)
        {
            float depth = float.PositiveInfinity;
            Water closestWater = null;
            foreach (var water in waters)
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

        public static IMyPlayer GetClosestPlayer(Vector3 position)
        {
            if (!MyAPIGateway.Multiplayer.MultiplayerActive)
                return MyAPIGateway.Session.Player;

            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players);

            float dist = float.MaxValue;
            IMyPlayer player = null;
            foreach (var playerLoop in players)
            {
                if (Vector3.DistanceSquared(position, player.GetPosition()) < dist)
                    player = playerLoop;
            }

            players.Clear();
            return player;
        }

        public override void BeforeStart()
        {
            MyAPIGateway.Utilities.SendModMessage(WaterModAPI.ModHandlerID, WaterModAPI.ModAPIVersion);
        }

        private void Entities_OnEntityAdd(IMyEntity obj)
        {
            if (obj is MyPlanet)
            {
                MyPlanet planet = obj as MyPlanet;

                foreach (var weather in planet.Generator.WeatherGenerators)
                {
                    if (weather.Voxel == "WATERMODDATA")
                    {
                        WaterSettings settings = MyAPIGateway.Utilities.SerializeFromXML<WaterSettings>(weather.Weathers?[0]?.Name);
                        Water water = new Water(planet);
                        water.radius = settings.Radius * planet.MinimumRadius;
                        water.waveHeight = settings.WaveHeight;
                        water.waveSpeed = settings.WaveSpeed;
                        water.viscosity = settings.Viscosity;
                        water.buoyancy = settings.Buoyancy;
                        waters.Add(water);
                        SyncClients();
                    }
                }
            }

            /*if (obj is MyCubeGrid)
            {
                MyCubeGrid grid = obj as MyCubeGrid;

                if(grid.IsRespawnGrid)
            }*/
        }

        //Commands
        private void Utilities_MessageEntered(string messageText, ref bool sendToOthers)
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

                    MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.WaterModVersion.Replace("{0}", WaterData.Version));
                    break;

                case "wlanguage":
                    sendToOthers = false;

                    //Set Language
                    if (args.Length == 2)
                    {
                        Language language;

                        if (WaterLocalization.Languages.TryGetValue(args[1], out language))
                        {
                            WaterLocalization.CurrentLanguage = language;

                            MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.SetLanguage.Replace("{0}", args[1]).Replace("{1}", WaterLocalization.CurrentLanguage.TranslationAuthor));
                            SaveSettings();
                        }
                        else
                            MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.SetLanguageNoParse.Replace("{0}", args[1]));
                    }
                    break;

                case "wquality":
                    sendToOthers = false;

                    //Get Quality
                    if (args.Length == 1)
                    {
                        MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.GetQuality.Replace("{0}", waterQuality.ToString()));
                    }

                    //Set Quality
                    if (args.Length == 2)
                    {
                        float quality = -1;
                        if (float.TryParse(args[1], out quality))
                        {
                            quality = MathHelper.Clamp(quality, 0.2f, 10f);
                            waterQuality = quality;

                            RecreateWater();
                            MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.SetQuality.Replace("{0}", waterQuality.ToString()));
                            SaveSettings();
                        }
                        else
                            MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.SetQualityNoParse.Replace("{0}", args[1]));
                    }
                    break;

                case "wcob":
                    sendToOthers = false;

                    //Toggle Center of Buoyancy Debug
                    if (args.Length == 1)
                    {
                        showCenterOfBuoyancy = !showCenterOfBuoyancy;
                        MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.ToggleRenderCOB);
                    }
                    break;

                case "wdepth":
                    sendToOthers = false;

                    //Toggle Show Depth
                    if (args.Length == 1)
                    {
                        showDepth = !showDepth;
                        MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.ToggleShowDepth);
                        SaveSettings();
                    }
                    break;
            }

            if (MyAPIGateway.Session.Player.PromoteLevel >= MyPromoteLevel.Admin)
            {
                switch (args[0])
                {
                    //Toggle fog
                    case "wfog":
                        sendToOthers = false;

                        showFog = !showFog;
                        MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.ToggleFog);
                        break;

                    //Toggle birds on a planet
                    case "wbird":
                        sendToOthers = false;

                        foreach (var water in waters)
                        {
                            if (water.planetID == closestPlanet.EntityId)
                            {
                                water.enableSeagulls = !water.enableSeagulls;
                                MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(waters));
                                MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.ToggleBirds);
                                return;
                            }
                        }

                        MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.NoPlanetWater);
                        break;

                    //Toggle fish on a planet
                    case "wfish":
                        sendToOthers = false;

                        foreach (var water in waters)
                        {
                            if (water.planetID == closestPlanet.EntityId)
                            {
                                water.enableFish = !water.enableFish;
                                MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(waters));
                                MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.ToggleFish);
                                return;
                            }
                        }

                        MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.NoPlanetWater);
                        break;

                    case "wbuoyancy":
                        sendToOthers = false;

                        //Get Buoyancy
                        if (args.Length == 1)
                        {
                            if (closestPlanet == null)
                            {
                                MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.NoPlanet);
                                return;
                            }

                            if (closestWater != null)
                            {
                                MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.GetBuoyancy.Replace("{0}", closestWater.buoyancy.ToString()));
                                return;
                            }

                            //If foreach loop doesn't find the planet, assume it doesn't exist
                            MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.NoPlanetWater);
                        }

                        //Set Buoyancy
                        if (args.Length == 2)
                        {

                            float buoyancy = -1;
                            if (float.TryParse(args[1], out buoyancy))
                            {
                                if (closestPlanet == null)
                                {
                                    MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.NoPlanet);
                                    return;
                                }

                                foreach (var water in waters)
                                {
                                    if (water.planetID == closestPlanet.EntityId)
                                    {
                                        water.buoyancy = buoyancy;
                                        MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(waters));
                                        MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.SetBuoyancy.Replace("{0}", water.buoyancy.ToString()));
                                        return;
                                    }
                                }

                                //If foreach loop doesn't find the planet, assume it doesn't exist
                                MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.NoPlanetWater);
                            }
                            else
                                MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.SetBuoyancyNoParse.Replace("{0}", args[1]));
                        }
                        break;

                    case "wviscosity":
                        sendToOthers = false;

                        //Get Viscosity
                        if (args.Length == 1)
                        {
                            if (closestPlanet == null)
                            {
                                MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.NoPlanet);
                                return;
                            }

                            if (closestWater != null)
                            {
                                MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.GetViscosity.Replace("{0}", closestWater.viscosity.ToString()));
                                return;
                            }

                            //If foreach loop doesn't find the planet, assume it doesn't exist
                            MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.NoPlanetWater);
                        }

                        //Set Viscosity
                        if (args.Length == 2)
                        {
                            float viscosity;
                            if (float.TryParse(args[1], out viscosity))
                            {
                                viscosity = MathHelper.Clamp(viscosity, 0f, 1f);
                                if (closestPlanet == null)
                                {
                                    MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.NoPlanet);
                                    return;
                                }

                                foreach (var water in waters)
                                {
                                    if (water.planetID == closestPlanet.EntityId)
                                    {
                                        water.viscosity = viscosity;
                                        MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(waters));
                                        MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.SetViscosity.Replace("{0}", water.viscosity.ToString()));
                                        return;
                                    }
                                }

                                //If foreach loop doesn't find the planet, assume it doesn't exist
                                MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.NoPlanetWater);
                            }
                            else
                                MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.SetViscosityNoParse.Replace("{0}", args[1]));
                        }
                        break;

                    case "wradius":
                        sendToOthers = false;

                        //Get Radius
                        if (args.Length == 1)
                        {
                            if (closestPlanet == null)
                            {
                                MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.NoPlanet);
                                return;
                            }

                            if (closestWater != null)
                            {
                                MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.GetRadius.Replace("{0}", (closestWater.radius / closestPlanet.MinimumRadius).ToString()));
                                return;
                            }

                            //If foreach loop doesn't find the planet, assume it doesn't exist
                            MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.NoPlanetWater);
                        }

                        //Set Radius
                        if (args.Length == 2)
                        {
                            float radius = -1;
                            if (float.TryParse(args[1], out radius))
                            {
                                radius = MathHelper.Clamp(radius, 0.01f, 10f);

                                if (closestPlanet == null)
                                {
                                    MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.NoPlanet);
                                    return;
                                }

                                foreach (var water in waters)
                                {
                                    if (water.planetID == closestPlanet.EntityId)
                                    {

                                        water.radius = radius * closestPlanet.MinimumRadius;
                                        MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(waters));
                                        MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.SetRadius.Replace("{0}", (water.radius / closestPlanet.MinimumRadius).ToString()));
                                        return;
                                    }
                                }

                                //If foreach loop doesn't find the planet, assume it doesn't exist
                                MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.NoPlanetWater);
                            }
                            else
                                MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.SetRadiusNoParse.Replace("{0}", args[1]));
                        }
                        break;

                    case "wwaveheight":
                        sendToOthers = false;

                        //Get Oscillation
                        if (args.Length == 1)
                        {
                            if (closestPlanet == null)
                            {
                                MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.NoPlanet);
                                return;
                            }

                            if (closestWater != null)
                            {
                                MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.GetWaveHeight.Replace("{0}", closestWater.waveHeight.ToString()));
                                return;

                            }

                            //If foreach loop doesn't find the planet, assume it doesn't exist
                            MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.NoPlanetWater);
                        }

                        //Set Oscillation
                        if (args.Length == 2)
                        {
                            float waveHeight = -1;

                            if (float.TryParse(args[1], out waveHeight))
                            {
                                if (closestPlanet == null)
                                {
                                    MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.NoPlanet);
                                    return;
                                }

                                foreach (var water in waters)
                                {
                                    if (water.planetID == closestPlanet.EntityId)
                                    {
                                        water.waveHeight = waveHeight;
                                        MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(waters));
                                        MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.SetWaveHeight.Replace("{0}", water.waveHeight.ToString()));
                                        return;
                                    }
                                }

                                //If foreach loop doesn't find the planet, assume it doesn't exist
                                MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.NoPlanetWater);
                            }
                            else
                                MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.SetWaveHeightNoParse.Replace("{0}", args[1]));
                        }
                        break;

                    case "wreset":
                        sendToOthers = false;

                        if (closestPlanet == null)
                        {
                            MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.NoPlanet);
                            return;
                        }

                        //Reset
                        for (int i = 0; i < waters.Count; i++)
                        {
                            if (waters[i].planetID == closestPlanet.EntityId)
                            {
                                float radius = waters[i].radius;
                                waters[i] = new Water(closestPlanet);
                                waters[i].radius = radius;

                                MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(waters));
                                MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.Reset);
                                return;
                            }
                        }

                        //If foreach loop doesn't find the planet, assume it doesn't exist
                        MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.NoPlanetWater);
                        break;

                    case "wspeed":
                        sendToOthers = false;

                        //Get Swing
                        if (args.Length == 1)
                        {
                            if (closestPlanet == null)
                            {
                                MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.NoPlanet);
                                return;
                            }

                            if (closestWater != null)
                            {
                                MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.GetWaveSpeed.Replace("{0}", closestWater.waveSpeed.ToString()));
                                return;
                            }

                            //If foreach loop doesn't find the planet, assume it doesn't exist
                            MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.NoPlanetWater);
                        }

                        //Set Swing
                        if (args.Length == 2)
                        {
                            float waveSpeed = -1;
                            if (float.TryParse(args[1], out waveSpeed))
                            {
                                if (closestPlanet == null)
                                {
                                    MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.NoPlanet);
                                    return;
                                }

                                foreach (var water in waters)
                                {
                                    if (water.planetID == closestPlanet.EntityId)
                                    {
                                        water.waveSpeed = MathHelper.Clamp(waveSpeed, 0f, 1f);
                                        MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(waters));
                                        MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.SetWaveSpeed.Replace("{0}", water.waveSpeed.ToString()));
                                        return;
                                    }
                                }

                                //If foreach loop doesn't find the planet, assume it doesn't exist
                                MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.NoPlanetWater);
                            }
                            else
                                MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.SetWaveSpeedNoParse.Replace("{0}", args[1]));
                        }
                        break;

                    case "wcreate":
                        sendToOthers = false;

                        if (closestPlanet == null)
                        {
                            MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.NoPlanet);
                            return;
                        }

                        foreach (var water in waters)
                        {
                            if (water.planetID == closestPlanet.EntityId)
                            {
                                MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.HasWater);
                                return;
                            }
                        }

                        waters.Add(new Water(closestPlanet));
                        MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(waters));
                        MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.CreateWater);
                        break;

                    case "wremove":
                        sendToOthers = false;

                        if (closestPlanet == null)
                        {
                            MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.NoPlanet);
                            return;
                        }
                        for (int i = waters.Count - 1; i >= 0; i--)
                        {
                            if (waters[i].planetID == closestPlanet.EntityId)
                            {
                                waters.RemoveAtFast(i);
                                MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(waters));
                                MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.RemoveWater);

                                for (int j = 0; j < waterLayers.Length; j++)
                                {
                                    if (waterLayers[j] == null)
                                        continue;

                                    waterLayers[j].Close();
                                    waterLayers[j] = null;
                                }
                                return;
                            }
                        }

                        //If foreach loop doesn't find the planet, assume it doesn't exist
                        MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.NoPlanetWater);
                        break;

                    case "wexport":
                        sendToOthers = false;

                        if (closestPlanet == null)
                        {
                            MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.NoPlanet);
                            return;
                        }

                        if (closestWater != null)
                        {
                            WaterSettings temp = new WaterSettings(closestWater);
                            MyClipboardHelper.SetClipboard(ValidateXMLData(MyAPIGateway.Utilities.SerializeToXML(temp)));
                            MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.ExportWater);
                            return;
                        }
                        MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.NoPlanetWater);
                        break;

                        /*case "wimport":
                            sendToOthers = false;

                            if (closestPlanet == null)
                            {
                                MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.NoPlanet);
                                return;
                            }

                            if (closestWater != null)
                            {
                                WaterSettings temp = MyAPIGateway.Utilities.SerializeFromXML<WaterSettings>(messageText.Substring(messageText.IndexOf(' ') + 1));
                                closestWater.buoyancy = temp.Buoyancy;
                                closestWater.radius = temp.Radius * closestWater.planet.MinimumRadius;
                                closestWater.viscosity = temp.Viscosity;
                                closestWater.waveHeight = temp.WaveHeight;
                                closestWater.waveSpeed = temp.WaveSpeed;
                                MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(waters));
                                return;
                            }

                            MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, WaterLocalization.CurrentLanguage.NoPlanetWater);
                            break;*/
                }
            }
        }

        public override void LoadData()
        {
            string packet;
            MyAPIGateway.Utilities.GetVariable("JWater", out packet);

            if (packet == null)
                return;

            waters = MyAPIGateway.Utilities.SerializeFromXML<List<Water>>(packet);

            LoadSettings();

            //Draygo
            DragAPI.Init();
        }

        public void LoadSettings()
        {
            //this is spaghetti
            if (!MyAPIGateway.Utilities.IsDedicated)
            {
                if (MyAPIGateway.Utilities.FileExistsInGlobalStorage("JakariaWaterSettings.cfg"))
                {
                    TextReader file = MyAPIGateway.Utilities.ReadFileInGlobalStorage("JakariaWaterSettings.cfg");
                    string line;

                    waterQuality = float.Parse(file.ReadLine());

                    Language language;
                    line = file.ReadLine();
                    if (line != null && WaterLocalization.Languages.TryGetValue(line, out language))
                        WaterLocalization.CurrentLanguage = language;
                    else
                        WaterLocalization.CurrentLanguage = WaterLocalization.Languages.TryGetValue(MyAPIGateway.Session.Config.Language.ToString().ToLower(), out language) ? language : WaterLocalization.Languages.GetValueOrDefault("english");

                    bool showDepthSetting;
                    if (bool.TryParse(file.ReadLine(), out showDepthSetting))
                    {
                        showDepth = showDepthSetting;
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
                writer.WriteLine(waterQuality);
                writer.WriteLine(WaterLocalization.CurrentLanguage.EnglishName);
                writer.WriteLine(showDepth);
                writer.Close();
            }
        }

        public override void SaveData()
        {
            MyAPIGateway.Utilities.SetVariable("JWater", MyAPIGateway.Utilities.SerializeToXML(waters));
        }

        protected override void UnloadData()
        {
            //Draygo
            DragAPI.Close();

            MyAPIGateway.Multiplayer.UnregisterMessageHandler(WaterData.ClientHandlerID, ClientHandler);
            MyAPIGateway.Utilities.MessageEntered -= Utilities_MessageEntered;

            MyVisualScriptLogicProvider.PlayerConnected -= PlayerConnected;
            MyAPIGateway.Entities.OnEntityAdd -= Entities_OnEntityAdd;
            //MyVisualScriptLogicProvider.RespawnShipSpawned -= RespawnShipSpawned;
            MyVisualScriptLogicProvider.PrefabSpawnedDetailed -= PrefabSpawnedDetailed;

            EnvironmentUnderwaterSoundEmitter.Cleanup();
            EnvironmentOceanSoundEmitter.Cleanup();
            EnvironmentBeachSoundEmitter.Cleanup();
            AmbientSoundEmitter.Cleanup();
            AmbientBoatSoundEmitter.Cleanup();
        }

        public override void UpdateAfterSimulation()
        {
            tickTimer++;
            //Delayed updating
            if (tickTimer > tickTimerMax)
            {
                tickTimer = 0;

                if (waters == null)
                    return;

                //Server and Client
                for (int i = waters.Count - 1; i >= 0; i--)
                {
                    MyPlanet planet = MyEntities.GetEntityById(waters[i].planetID) as MyPlanet;
                    if (planet == null)
                    {
                        waters.RemoveAtFast(i);
                        continue;
                    }
                    else
                        waters[i].planet = planet;
                }

                //Server
                if (MyAPIGateway.Session.IsServer)
                {
                    foreach (var water in waters)
                    {
                        BoundingSphereD sphere = new BoundingSphereD(water.position, water.currentRadius);
                        water.underWaterEntities.Clear();
                        MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref sphere, water.underWaterEntities, MyEntityQueryType.Dynamic);
                    }

                    List<MyObjectBuilder_WeatherPlanetData> weatherPlanets = MyAPIGateway.Session.WeatherEffects.GetWeatherPlanetData();
                    for (int i = weatherPlanets.Count - 1; i >= 0; i--)
                    {
                        foreach (var water in waters)
                        {
                            if (water.planetID == weatherPlanets[i].PlanetId)
                            {
                                for (int j = weatherPlanets[i].Weathers.Count - 1; j >= 0; j--)
                                {
                                    if (water.IsUnderwater(weatherPlanets[i].Weathers[j].Position))
                                        MyAPIGateway.Session.WeatherEffects.RemoveWeather(weatherPlanets[i].Weathers[j]);
                                }
                            }
                        }
                    }
                }

                //Client
                if (!MyAPIGateway.Utilities.IsDedicated)
                {
                    nightValue = 1 - (Math.Max(-GetNightValue(closestPlanet, MyAPIGateway.Session.Camera.Position), 0) / 2);

                    if (TextAPI.Heartbeat && DepthMeter.Visible)
                    {
                        DepthMeter.Message = new StringBuilder("\n\n\n" + WaterLocalization.CurrentLanguage.Depth.Replace("{0}", Math.Round(-depth).ToString()));
                        DepthMeter.Offset = new Vector2D(-DepthMeter.GetTextLength().X / 2, 0);
                    }

                    if (closestWater != null && closestPlanet != null)
                    {
                        MyAPIGateway.Session.WeatherEffects.GetWeather(MyAPIGateway.Session.Camera.Position, out closestWeather);

                        BoundingSphereD sphere = new BoundingSphereD(MyAPIGateway.Session.Camera.Position, 1000);
                        nearbyEntities.Clear();
                        MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref sphere, nearbyEntities);

                        if (nearbyEntities != null)
                            foreach (var entity in nearbyEntities)
                            {
                                if (entity == null || entity.Physics == null || !entity.Physics.Enabled)
                                    continue;

                                float capacity = 0;
                                Vector3D center = Vector3.Zero;
                                float litres = 0;

                                Vector3D velocity = entity.Physics.LinearVelocity;
                                Vector3D verticalVelocity = Vector3D.ProjectOnVector(ref velocity, ref gravityDirection);
                                Vector3D horizontalVelocity = velocity - verticalVelocity;

                                if (entity is MyCubeGrid)
                                {
                                    foreach (var block in (entity as MyCubeGrid).GetFatBlocks())
                                    {
                                        if (block == null)
                                            continue;

                                        bool belowWater = closestWater.IsUnderwater(block.PositionComp.GetPosition());
                                        if (!block.IsFunctional)
                                        {
                                            if (block.IsBuilt && belowWater)
                                            {
                                                block.StopDamageEffect(true);

                                                if (block.HasInventory)
                                                    if (MyUtils.GetRandomInt(0, 100) < 15)
                                                        lock (effectLock)
                                                            bubbles.Add(new Bubble(block.PositionComp.GetPosition(), block.CubeGrid.GridSizeHalf));
                                            }
                                            continue;
                                        }

                                        if (belowWater)
                                        {

                                            if (block is MyThrust)
                                            {
                                                MyThrust thruster = block as MyThrust;

                                                if (thruster == null || !thruster.IsPowered || Vector3D.Distance(closestWater.position, thruster.PositionComp.GetPosition()) > closestWater.currentRadius)
                                                    continue;

                                                if (thruster.BlockDefinition.ThrusterType.String.Equals("Atmospheric") && thruster.CurrentStrength >= MyUtils.GetRandomFloat(0f, 1f) && MyUtils.GetRandomInt(0, 2) == 1)
                                                {
                                                    bubbles.Add(new Bubble(thruster.PositionComp.GetPosition(), thruster.CubeGrid.GridSize));
                                                }
                                            }

                                            if (block is IMyGasTank)
                                            {
                                                IMyGasTank gasTank = block as IMyGasTank;
                                                capacity = (float)gasTank.FilledRatio * gasTank.Capacity;
                                                litres += capacity;
                                                center += block.Position * capacity;
                                            }
                                        }
                                    }

                                    Vector3D centerOfBuoyancy;

                                    if (litres == 0)
                                        centerOfBuoyancy = entity.Physics.CenterOfMassWorld;
                                    else
                                        centerOfBuoyancy = (entity as MyCubeGrid).GridIntegerToWorld(center / litres);

                                    if (showCenterOfBuoyancy && !Vector3.IsZero(entity.Physics.LinearVelocity))
                                        indicators.Add(new COBIndicator(centerOfBuoyancy));

                                    float depth = Vector3.Distance(closestWater.position, centerOfBuoyancy) - closestWater.currentRadius;

                                    if (depth < 1.3f && depth > -5)
                                    {
                                        if (horizontalVelocity.Length() > 5f) //Horizontal Velocity
                                        {
                                            wakes.Add(new Wake(closestWater.GetClosestSurfacePoint(centerOfBuoyancy, -0.5f), horizontalVelocity * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS_HALF, gravityDirection, Math.Min((float)entity.PositionComp.LocalVolume.GetBoundingBox().Depth, Math.Min((float)entity.PositionComp.LocalVolume.GetBoundingBox().Width, (float)entity.PositionComp.LocalVolume.GetBoundingBox().Height)) / 4, Math.Min((float)entity.PositionComp.LocalVolume.GetBoundingBox().Depth, Math.Min((float)entity.PositionComp.LocalVolume.GetBoundingBox().Width, (float)entity.PositionComp.LocalVolume.GetBoundingBox().Height)) / 4));
                                        }

                                        if (verticalVelocity.Length() > 2f) //Vertical Velocity
                                        {
                                            if (entity is MyCubeGrid)
                                                splashes.Add(new Splash(closestWater.GetClosestSurfacePoint(centerOfBuoyancy), entity.PositionComp.LocalVolume.Radius));
                                            else
                                                splashes.Add(new Splash(closestWater.GetClosestSurfacePoint(centerOfBuoyancy), entity.Physics.Speed));
                                        }

                                    }
                                }
                            }
                    }
                }
            }

            SimulatePhysics();

            //Clients
            try
            {
                if (!MyAPIGateway.Utilities.IsDedicated && initialized && MyAPIGateway.Session != null && MyAPIGateway.Session.Player != null && MyAPIGateway.Session.Camera != null)
                {
                    SimulateSounds();

                    insideGrid = MyAPIGateway.Session?.Player?.Character?.Components?.Get<MyEntityReverbDetectorComponent>() != null ? MyAPIGateway.Session.Player.Character.Components.Get<MyEntityReverbDetectorComponent>().Grids : 0;
                    if (MyAPIGateway.Input.IsNewGameControlPressed(MyControlsSpace.TOGGLE_HUD))
                        showHud = !MyAPIGateway.Session?.Config?.MinimalHud ?? true;

                    closestPlanet = MyGamePruningStructure.GetClosestPlanet(MyAPIGateway.Session.Camera.Position);
                    closestWater = GetClosestWater(MyAPIGateway.Session.Camera.Position);

                    if (closestPlanet != null)
                    {
                        gravityDirection = Vector3D.Normalize(closestPlanet.PositionComp.GetPosition() - MyAPIGateway.Session.Camera.Position);
                    }

                    if (closestWater != null)
                        MyAPIGateway.Parallel.StartBackground(SimulateEffects);
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine(e);
                MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, e.ToString());
            }
        }

        public void SimulatePhysics()
        {

            if (MyAPIGateway.Session.IsServer)
            {
                foreach (var water in waters)
                {
                    water.waveTimer++;
                    water.currentRadius = (float)Math.Max(water.radius + (Math.Sin(water.waveTimer * water.waveSpeed) * water.waveHeight), 0);

                    foreach (var entity in water.underWaterEntities)
                    {
                        if (entity == null || entity.Physics == null || entity.Physics.IsStatic || entity.MarkedForClose || entity.IsPreview || !(entity is MyCubeGrid))
                            continue;

                        SimulateGrid(water, entity as MyCubeGrid);
                    }
                }
            }

            //Clients, no host
            if (!MyAPIGateway.Session.IsServer)
            {
                if (closestWater != null)
                {
                    closestWater.waveTimer++;
                    closestWater.currentRadius = (float)Math.Max(closestWater.radius + (Math.Sin(closestWater.waveTimer * closestWater.waveSpeed) * closestWater.waveHeight), 0);

                    if (MyAPIGateway.Session.ControlledObject?.Entity is MyCockpit)
                    {
                        (MyAPIGateway.Session.ControlledObject as MyCockpit).CubeGrid.ForceDisablePrediction = true;
                    }
                }
            }
        }

        private MyEntity CreateWaterMesh() //Water layer mesh
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
            return ent;
        }

        private MyEntity CreateWater2Mesh() //Solid/Non-transparent version of water mesh
        {
            var ent = new MyEntity();
            ent.Init(null, ModContext.ModPath + @"\Models\Water2.mwm", null, null, null);
            ent.Render.CastShadows = false;
            ent.Render.DrawOutsideViewDistance = true;
            ent.IsPreview = true;
            ent.Save = false;
            ent.SyncFlag = false;
            ent.NeedsWorldMatrix = false;
            ent.Flags |= EntityFlags.IsNotGamePrunningStructureObject;
            MyEntities.Add(ent, false);
            return ent;
        }

        public void SimulateGrid(Water water, MyCubeGrid grid)
        {
            if (grid == null || grid.Physics == null || grid.Physics.IsStatic)
                return;

            Vector3D center = Vector3.Zero;
            double litres = 0;
            float capacity = 0;
            float depth = Vector3.Distance(water.position, grid.PositionComp.GetPosition());
            float tankHealth = 0;
            float totalTanks = 0;

            //Vector3 dragForce = -(Vector3.Normalize(grid.Physics.LinearVelocity) * (grid.Physics.Speed * grid.Physics.Speed)) * water.viscosity * grid.Physics.Mass * (1f + (Math.Max(-(Vector3.Distance(water.position, grid.Physics.CenterOfMassWorld) - water.currentRadius), 0) / 2000f));
            Vector3 dragForce = -(grid.Physics.LinearVelocity * grid.Physics.Mass * water.viscosity * (1 + (Math.Min(-water.GetDepth(grid.Physics.CenterOfMassWorld), 0) / 2000f))) * grid.Physics.Speed;

            //Drag (Section written by Draygo)
            if (DragAPI.Heartbeat)
            {
                var api = dragAPIGetter.GetOrAdd(grid, DragClientAPI.DragObject.Factory);
                if (api != null)
                {
                    //should increase the viscositymultiplier so its 5x more drag than air. Let the drag mod run the drag calculation. 
                    //api.ViscosityMultiplier = 1000;
                    api.ViscosityMultiplier = 1f + (water.viscosity * 1000f * (1f + (Math.Max(-(Vector3.Distance(water.position, grid.Physics.CenterOfMassWorld) - water.currentRadius), 0) / 2000f)));
                    dragForce /= 4;

                    if (grid.Physics.AngularVelocity.IsValid())
                        grid.Physics.AngularVelocity *= 1f - ((water.viscosity * (grid.Physics.Speed / 20f)) / 4);
                }
            } //End Draygo
            else if (grid.Physics.AngularVelocity.IsValid())
                grid.Physics.AngularVelocity *= 1f - (water.viscosity * (grid.Physics.Speed / 20f));

            if (dragForce.IsValid())
                grid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, dragForce / 2, null, null, null, false, false);
            //End Drag

            foreach (var block in grid.GetFatBlocks())
            {
                if (block is IMyGasTank)
                {
                    totalTanks++;
                    tankHealth += 1 - (block.SlimBlock as IMySlimBlock).CurrentDamage / (block.SlimBlock as IMySlimBlock).MaxIntegrity;

                    if (!block.IsFunctional)
                        continue;

                    float depth2 = MathHelper.Min((water.currentRadius + 0.5f) - Vector3.Distance(water.position, block.PositionComp.GetPosition()), 1);

                    if (depth2 > 0f)
                    {
                        IMyGasTank gasTank = block as IMyGasTank;
                        capacity = (float)gasTank.FilledRatio * gasTank.Capacity * depth2;
                        litres += capacity;
                        center += block.Position * capacity;
                    }
                }
            }

            if (litres != 0)
            {
                Vector3D centerOfBuoyancy = grid.GridIntegerToWorld(center / litres);

                double force = 0;
                if (grid.GridSizeEnum == MyCubeSize.Large)
                    force = litres * (1 + ((water.currentRadius - depth) / 5000f)) / 50f * (Math.Pow(tankHealth / totalTanks, 2) * water.buoyancy);
                else
                    force = litres * (1 + ((water.currentRadius - depth) / 5000f)) / 20f * (Math.Pow(tankHealth / totalTanks, 2) * water.buoyancy);

                //force -= Math.Abs(Vector3.Dot(grid.Physics.LinearVelocity, Vector3D.Normalize(grid.Physics.Gravity))) * (grid.Physics.Speed * grid.Physics.Mass) * 0.01;
                force += Vector3.Dot(grid.Physics.LinearVelocity, Vector3D.Normalize(grid.Physics.Gravity)) * (grid.Physics.Speed * grid.Physics.Mass) * 0.01;

                grid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, -Vector3D.Normalize(grid.Physics.Gravity) * force * 60, centerOfBuoyancy, null, null, true, false);
            }
        }

        public override void Draw()
        {
            Vector3D grav0 = Vector3.Forward;
            Vector3D grav90 = Vector3.Up;

            if (gravityDirection != Vector3D.Zero)
            {
                grav0 = Vector3.Normalize(Vector3D.Cross(gravityDirection, Vector3D.Forward));
                grav90 = Vector3.Normalize(Vector3D.Cross(gravityDirection, Vector3D.Up));
            }

            //Text Hud API Update
            if (TextAPI.Heartbeat)
            {
                DepthMeter.Visible = showDepth && depth < 0;
            }

            foreach (var water in waters)
            {
                if (water == null || water.planet == null)
                    continue;

                MatrixD worldMatrix = water.planet.WorldMatrix;

                if (water == closestWater)
                {
                    Color white2 = new Color(0.4f, 0.4f, 0.4f, 0.9f * (MathHelper.Clamp(depth - 25f, 0, 300) / 300f));
                    MySimpleObjectDraw.DrawTransparentSphere(ref worldMatrix, water.currentRadius, ref white2, MySimpleObjectRasterizer.Solid, 35, faceMaterial: WaterData.WaterMaterial);
                }
                else
                {
                    Color white2 = new Color(0.4f, 0.4f, 0.4f, 0.8f);
                    MySimpleObjectDraw.DrawTransparentSphere(ref worldMatrix, water.currentRadius, ref white2, MySimpleObjectRasterizer.Solid, 35, faceMaterial: WaterData.WaterMaterial);
                }

            }

            if (closestWater != null)
            {
                depth = closestWater.GetDepth(MyAPIGateway.Session.Camera.Position);
                underwater = depth <= 0;

                if (underwater)
                    MyTransparentGeometry.AddBillboardOriented(WaterData.WaterMaterial, new Vector4(1, 1, 1, 0.9f), closestWater.position - (gravityDirection * closestWater.currentRadius), grav0, grav90, 50000);
            }
            else
            {
                depth = 0;
                underwater = false;
            }

            double seperation = ((double)waterLayerSeperation / waterQuality) + (depth / 100);

            for (int i = 0; i < waterLayers.Length; i++)
            {
                if (waterLayers[i] == null)
                    continue;

                waterLayers[i].Render.Visible = !underwater && closestWater != null;

                if (!underwater && closestWater != null)
                {
                    double radius = (closestWater.currentRadius - ((i + 1) * seperation));

                    waterLayers[i].PositionComp.Scale = (float)Math.Sqrt((closestWater.radius * closestWater.radius) - (radius * radius));
                    waterLayers[i].WorldMatrix = MatrixD.CreateWorld(closestWater.position - (gravityDirection * (radius + seperation)), grav0, -gravityDirection);

                    waterLayers[i].InScene = true;
                    waterLayers[i].Render.UpdateRenderObject(true, false);
                }
            }

            if (underwater)
            {
                if (showFog)
                    MyAPIGateway.Session.WeatherEffects.FogDensityOverride = 0.015f * (1f + (-depth / 100f)) * ((25f - Math.Max(insideGrid - 10, 0)) / 25f);
                else
                    MyAPIGateway.Session.WeatherEffects.FogDensityOverride = 0.001f;

                MyAPIGateway.Session.WeatherEffects.FogMultiplierOverride = 1;
                MyAPIGateway.Session.WeatherEffects.FogColorOverride = Vector3D.Lerp(new Vector3D(0.1, 0.125, 0.176), Vector3D.Zero, -depth / 1000.0);
                MyAPIGateway.Session.WeatherEffects.FogAtmoOverride = 1;
                MyAPIGateway.Session.WeatherEffects.FogSkyboxOverride = 1;
            }

            //Only Change fog once above water and all the time underwater
            if (previousUnderwaterState != underwater)
            {
                previousUnderwaterState = underwater;

                if (underwater)
                {
                    //MyAPIGateway.Session.WeatherEffects.FogColorOverride = Vector3D.Lerp(new Vector3D(0.1, 0.125, 0.176), Vector3D.Zero, -depth / 1000.0);
                    MyAPIGateway.Session.WeatherEffects.SunIntensityOverride = 0.1f;
                    MyAPIGateway.Session.WeatherEffects.ParticleVelocityOverride = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                }
                else
                {
                    MyVisualScriptLogicProvider.FogSetAll(null, null, null, null, null);
                    MyAPIGateway.Session.WeatherEffects.SunIntensityOverride = null;
                    MyAPIGateway.Session.WeatherEffects.ParticleVelocityOverride = null;
                }
            }

            lock (effectLock)
            {
                float lifeRatio = 0;

                foreach (var bubble in bubbles)
                {
                    if (bubble == null)
                        continue;

                    MyTransparentGeometry.AddPointBillboard(WaterData.BubblesMaterial, WaterData.BubbleColor * (1f - ((float)bubble.life / (float)bubble.maxLife)), bubble.position, bubble.radius, bubble.angle);
                }

                if (showHud)
                    foreach (var indicator in indicators)
                    {
                        if (indicator == null)
                            continue;

                        MyTransparentGeometry.AddPointBillboard(WaterData.IconMaterial, white, indicator.position, 1, 0, blendType: BlendTypeEnum.AdditiveTop);
                    }

                if (underwater)
                {
                    foreach (var bubble in smallBubbles)
                    {
                        if (bubble == null)
                            continue;

                        MyTransparentGeometry.AddPointBillboard(WaterData.FireflyMaterial, bubble.color * (1f - ((float)bubble.life / bubble.maxLife)), bubble.position, bubble.radius, bubble.angle);
                    }

                    if (depth > -1000 && closestWater?.enableFish == true)
                        foreach (var fish in fishes)
                        {
                            if (fish == null)
                                continue;

                            MyTransparentGeometry.AddBillboardOriented(WaterData.FishMaterials[fish.textureId], white, fish.position, fish.leftVector, fish.upVector, 0.7f);
                        }
                }
                else
                {
                    foreach (var splash in splashes)
                    {
                        if (splash == null)
                            continue;

                        lifeRatio = (float)splash.life / splash.maxLife;
                        MyTransparentGeometry.AddBillboardOriented(WaterData.SplashMaterial, new Vector4(2 * nightValue, 2 * nightValue, 2 * nightValue, 1) * Math.Min(1f - lifeRatio, 1), splash.position, grav0, grav90, 2f + splash.radius * lifeRatio);
                    }

                    Vector4 wakeColor = new Vector4(nightValue, nightValue, nightValue, 0.25f);

                    foreach (var wake in wakes)
                    {
                        if (wake == null)
                            continue;

                        lifeRatio = (float)Math.Sin((float)wake.life / wake.maxLife);
                        MyTransparentGeometry.AddBillboardOriented(WaterData.WakeMaterial, wakeColor * (float)Math.Abs(1f - lifeRatio), wake.position, wake.leftVector, wake.upVector, (wake.minimumRadius / 2) + wake.maximumRadius * lifeRatio);
                    }

                    if (closestWeather == null && closestWater?.enableSeagulls == true)
                        foreach (var seagull in seagulls)
                        {
                            if (seagull == null)
                                continue;

                            //MyTransparentGeometry.AddPointBillboard(seagullMaterial, white, seagull.position, 1, 0);
                            MyTransparentGeometry.AddBillboardOriented(WaterData.SeagullMaterial, white, seagull.position, seagull.leftVector, seagull.upVector, 0.7f);
                        }
                }
            }
        }

        public void RecreateWater()
        {
            if (waterLayers.Length != waterLayerAmount * waterQuality)
            {
                for (int i = 0; i < waterLayers.Length; i++)
                {
                    if (waterLayers[i] == null)
                        continue;

                    waterLayers[i].Close();
                }

                waterLayers = new MyEntity[(int)(waterLayerAmount * waterQuality)];

                smallBubbles = new SmallBubble[(int)Math.Min(128 * waterQuality, 128)];

                foreach (var seagull in seagulls)
                {
                    if (seagull?.cawSound?.IsPlaying == true)
                        seagull.cawSound.StopSound(true, true);
                }

                seagulls = new Seagull[(int)Math.Min(256 * waterQuality, 128)];
                fishes = new Fish[(int)Math.Min(128 * waterQuality, 64)];
                //fishes = new Fish[8192];
            }

            MyAPIGateway.Parallel.For(0, waterLayers.Length, i =>
            {
                try
                {
                    if (waterLayers[i] == null)
                    {
                        if (i < (int)(waterLayerAmount * waterQuality) - 1)
                            waterLayers[i] = CreateWaterMesh();
                        else
                            waterLayers[i] = CreateWater2Mesh();
                    }
                }
                catch (Exception e)
                {
                    MyLog.Default.WriteLine(e);
                    MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, e.ToString());
                }
            });
        }

        public void SimulateSounds()
        {
            if (closestPlanet != null && closestWater != null)
            {
                if (MyAPIGateway.Session?.Camera?.Position != null)
                {
                    if (AmbientBoatSoundEmitter == null || AmbientSoundEmitter == null || EnvironmentOceanSoundEmitter == null || EnvironmentBeachSoundEmitter == null || EnvironmentUnderwaterSoundEmitter == null)
                        return;

                    if (underwater)
                    {
                        ambientTimer--;
                        if (ambientTimer <= 0)
                        {
                            ambientTimer = MyUtils.GetRandomInt(1000, 1900); //Divide by 60 to get in seconds

                            if (!AmbientSoundEmitter.IsPlaying)
                            {
                                AmbientSoundEmitter.PlaySound(WaterData.AmbientSound);
                                AmbientSoundEmitter.SetPosition(MyAPIGateway.Session.Camera.Position + (MyUtils.GetRandomVector3Normalized() * MyUtils.GetRandomFloat(0, 75)));
                            }
                        }

                        if (!EnvironmentUnderwaterSoundEmitter.IsPlaying)
                            EnvironmentUnderwaterSoundEmitter.PlaySound(WaterData.EnvironmentUnderwaterSound, force2D: true);

                        if (EnvironmentBeachSoundEmitter.IsPlaying)
                            EnvironmentBeachSoundEmitter.StopSound(true, false);

                        if (EnvironmentOceanSoundEmitter.IsPlaying)
                            EnvironmentOceanSoundEmitter.StopSound(true, false);
                    }
                    else
                    {
                        ambientBoatTimer--;
                        if (ambientBoatTimer <= 0)
                        {
                            ambientBoatTimer = MyUtils.GetRandomInt(2000, 3500); //Divide by 60 to get in seconds

                            if (underwater && !AmbientBoatSoundEmitter.IsPlaying && insideGrid > 10)
                            {
                                AmbientBoatSoundEmitter.PlaySound(WaterData.AmbientSound);
                                AmbientBoatSoundEmitter.SetPosition(MyAPIGateway.Session.Camera.Position + (MyUtils.GetRandomVector3Normalized() * MyUtils.GetRandomFloat(0, 75)));
                            }
                        }

                        if (!EnvironmentOceanSoundEmitter.IsPlaying)
                            EnvironmentOceanSoundEmitter.PlaySound(WaterData.EnvironmentOceanSound);

                        //EnvironmentBeachSoundEmitter.SetPosition(closestPlanet.GetClosestSurfacePointGlobal(MyAPIGateway.Session.Camera.Position));

                        if (!EnvironmentBeachSoundEmitter.IsPlaying)
                            EnvironmentBeachSoundEmitter.PlaySound(WaterData.EnvironmentBeachSound, force2D: true);

                        if (closestWeather == null && seagulls != null)
                            foreach (var seagull in seagulls)
                            {
                                if (seagull == null)
                                    continue;

                                if (MyUtils.GetRandomInt(0, 100) < 1)
                                {
                                    seagull.Caw();
                                }
                            }

                        if (EnvironmentUnderwaterSoundEmitter.IsPlaying)
                            EnvironmentUnderwaterSoundEmitter.StopSound(true, false);
                    }

                    float volumeMultiplier = (25f - Math.Max(insideGrid - 5, 0)) / 25f;
                    EnvironmentUnderwaterSoundEmitter.VolumeMultiplier = volumeMultiplier;
                    EnvironmentOceanSoundEmitter.VolumeMultiplier = volumeMultiplier * MyMath.Clamp((200f - depth) / 200f, 0, 1f);
                    EnvironmentBeachSoundEmitter.VolumeMultiplier = volumeMultiplier * MyMath.Clamp((100f - depth) / 100f, 0, 1f);
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

        public void SimulateEffects()
        {
            try
            {
                lock (effectLock)
                {
                    if (bubbles != null && bubbles.Count > 0)
                        for (int i = bubbles.Count - 1; i >= 0; i--)
                        {
                            if (bubbles[i] == null || bubbles[i].life > bubbles[i].maxLife || closestPlanet == null)
                            {
                                bubbles.RemoveAtFast(i);
                                continue;
                            }
                            bubbles[i].position += -gravityDirection * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS / 2;
                            bubbles[i].life++;
                        }
                    if (splashes != null && splashes.Count > 0)
                        for (int i = splashes.Count - 1; i >= 0; i--)
                        {
                            if (splashes[i] == null || splashes[i].life > splashes[i].maxLife || closestPlanet == null)
                            {
                                splashes.RemoveAtFast(i);
                                continue;
                            }
                            splashes[i].life++;
                        }


                    if (wakes != null && wakes.Count > 0)
                        for (int i = wakes.Count - 1; i >= 0; i--)
                        {
                            if (wakes[i] == null || wakes[i].life > wakes[i].maxLife || closestPlanet == null)
                            {
                                wakes.RemoveAtFast(i);
                                continue;
                            }
                            wakes[i].position += wakes[i].velocity;
                            wakes[i].life++;
                            wakes[i].velocity *= 0.99f;
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

                    if (smallBubbles != null && underwater)
                        for (int i = smallBubbles.Length - 1; i >= 0; i--)
                        {
                            if (smallBubbles[i] == null || smallBubbles[i].life > smallBubbles[i].maxLife)
                            {
                                smallBubbles[i] = new SmallBubble(MyAPIGateway.Session.Camera.Position + (MyUtils.GetRandomVector3Normalized() * MyUtils.GetRandomFloat(5, 20)));
                                continue;
                            }
                            smallBubbles[i].position += -gravityDirection * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS / 2;
                            smallBubbles[i].life++;
                        }

                    if (seagulls != null && closestWater?.enableSeagulls == true)
                        for (int i = 0; i < seagulls.Length; i++)
                        {
                            if (seagulls[i] == null || seagulls[i].life > seagulls[i].maxLife || closestPlanet == null)
                            {
                                if (seagulls[i]?.cawSound?.IsPlaying == true)
                                    continue;

                                seagulls[i] = new Seagull(closestWater.GetClosestSurfacePoint(MyAPIGateway.Session.Camera.Position) + (-gravityDirection * MyUtils.GetRandomFloat(25, 150)) + (MyUtils.GetRandomPerpendicularVector(gravityDirection) * MyUtils.GetRandomFloat(-2500, 2500)), MyUtils.GetRandomPerpendicularVector(gravityDirection) * MyUtils.GetRandomFloat(0.05f, 0.15f), -gravityDirection);
                                continue;
                            }

                            seagulls[i].life++;
                            seagulls[i].position += seagulls[i].velocity;
                        }

                    if (fishes != null && closestWater?.enableFish == true && depth > -1000)
                        for (int i = 0; i < fishes.Length; i++)
                        {
                            if (fishes[i] == null || fishes[i].life > fishes[i].maxLife || closestPlanet == null)
                            {
                                fishes[i] = new Fish(MyAPIGateway.Session.Camera.Position + (MyUtils.GetRandomVector3Normalized() * MyUtils.GetRandomFloat(50, 250)), MyUtils.GetRandomPerpendicularVector(gravityDirection) * MyUtils.GetRandomFloat(0.05f, 0.11f), gravityDirection);
                                continue;
                            }

                            fishes[i].life++;
                            fishes[i].position += fishes[i].velocity;
                        }
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine(e);
                MyAPIGateway.Utilities.ShowMessage(WaterLocalization.ModChatName, e.ToString());
            }
        }

        public static Vector3D GetPerpendicularVector(Vector3D vector, double angle)
        {
            Vector3D perpVector = Vector3D.CalculatePerpendicularVector(Vector3.Normalize(vector));
            Vector3D bitangent; Vector3D.Cross(ref vector, ref perpVector, out bitangent);
            return Vector3D.Normalize(Math.Cos(angle) * perpVector + Math.Sin(angle) * bitangent);
        }

        private void PlayerConnected(long playerId)
        {
            if (MyAPIGateway.Session.IsServer)
                SyncClients();
        }

        public void SyncClients()
        {
            if (MyAPIGateway.Session.IsServer && waters != null)
            {
                MyAPIGateway.Multiplayer.SendMessageToOthers(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(waters));
                MyAPIGateway.Utilities.SendModMessage(WaterModAPI.ModHandlerID, MyAPIGateway.Utilities.SerializeToBinary(waters));

            }
        }

        private void ClientHandler(byte[] packet)
        {
            waters = MyAPIGateway.Utilities.SerializeFromBinary<List<Water>>(packet);

            if (!MyAPIGateway.Utilities.IsDedicated)
                RecreateWater();

            if (MyAPIGateway.Session.IsServer)
                SyncClients();
        }

        public static string ValidateXMLData(string input)
        {
            input = input.Replace("<", "&lt;");
            input = input.Replace(">", "&gt;");
            return input;
        }

        public float GetNightValue(MyPlanet planet, Vector3 position)
        {
            if (planet == null)
                return 0;

            return Vector3.Dot(MyVisualScriptLogicProvider.GetSunDirection(), Vector3.Normalize(position - planet.PositionComp.GetPosition()));
        }
    }
}