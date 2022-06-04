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
using VRage.Game.ModAPI;
using VRage.ModAPI;
using Sandbox.Game.EntityComponents;
using System.IO;
using Draygo.Drag.API;
using System.Collections.Concurrent;
using System.Linq;
using VRage;
using Jakaria.Utils;
using Jakaria.API;
using VRage.Serialization;
using Sandbox.Definitions;
using ProtoBuf;
using VRageRender;
using Jakaria.Configs;
using Jakaria.Components;
using Jakaria.SessionComponents;

namespace Jakaria.SessionComponents
{
    /// <summary>
    /// Base entry mod component, manages everything
    /// </summary>
    public class WaterModComponent : SessionComponentBase
    {
        public Dictionary<long, Water> Waters = new Dictionary<long, Water>();

        public Action UpdateAfter1;
        public Action UpdateAfter60;

        public ConcurrentDictionary<IMyEntity, DragClientAPI.DragObject> DragObjects = new ConcurrentDictionary<IMyEntity, DragClientAPI.DragObject>();
        public DragClientAPI DragClientAPI = new DragClientAPI();

        private int _tickTimerSecond = 0;

        private WaterManagerComponent _managerComponent;

        public static WaterModComponent Static;

        public WaterModComponent()
        {
            Static = this;
            UpdateOrder = MyUpdateOrder.AfterSimulation;
        }

        public override void LoadDependencies()
        {
            _managerComponent = WaterManagerComponent.Static;
        }

        public override void UnloadDependencies()
        {
            _managerComponent = null;

            Static = null;
        }

        public override void BeforeStart()
        {
            WaterModAPIBackend.BeforeStart();
        }

        public override void Init()
        {
            WaterUtils.ShowMessage(String.Format(WaterLocalization.CurrentLanguage.WaterModVersion, WaterData.Version + (WaterData.EarlyAccess ? "EA" : "")));
            WaterUtils.WriteLog(String.Format(WaterLocalization.CurrentLanguage.WaterModVersion, WaterData.Version + (WaterData.EarlyAccess ? "EA" : "")));

            MyAPIGateway.Multiplayer.RegisterMessageHandler(WaterData.ClientHandlerID, ClientHandler);

            MyEntities.OnEntityCreate += Entities_OnEntityCreate;
            MyEntities.OnEntityRemove += MyEntities_OnEntityRemove;

            //Server
            if (MyAPIGateway.Session.IsServer)
            {
                MyEntities.OnEntityAdd += Entities_OnEntityAdd;
                MyVisualScriptLogicProvider.PlayerConnected += PlayerConnected;
            }

            if (!MyAPIGateway.Session.SessionSettings.EnableOxygen || !MyAPIGateway.Session.SessionSettings.EnableOxygenPressurization)
            {
                WaterUtils.ShowMessage("Oxygen/Airtightness is disabled, Airtightness flotation will not work.");
                WaterUtils.WriteLog("Oxygen/Airtightness is disabled, Airtightness flotation will not work.");
            }
        }

        private void MyEntities_OnEntityRemove(MyEntity obj)
        {
            if (obj is MyPlanet)
            {
                if(Waters.ContainsKey(obj.EntityId))
                    Waters.Remove(obj.EntityId);
            }
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

        private void Entities_OnEntityAdd(MyEntity entity)
        {
            if (entity != null)
            {
                AddPhysicsComponentToEntity(entity);

                if (MyAPIGateway.Session.IsServer)
                {
                    if (entity is MyPlanet)
                    {
                        MyPlanet planet = entity as MyPlanet;

                        if (WaterUtils.HasWater(planet))
                            return;

                        PlanetConfig config;

                        if (WaterData.PlanetConfigs.TryGetValue(planet.Generator.Id, out config))
                        {
                            if (config.WaterSettings != null)
                            {
                                Waters[planet.EntityId] = new Water(planet, config.WaterSettings);
                                SyncClients();
                            }
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

        /// <summary>
        /// Called when the world is loading
        /// </summary>
        public override void LoadData()
        {
            DragClientAPI.Init();

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
        public override void UnloadData()
        {
            MyAPIGateway.Multiplayer.UnregisterMessageHandler(WaterData.ClientHandlerID, ClientHandler);
            MyEntities.OnEntityAdd -= Entities_OnEntityCreate;
            MyEntities.OnEntityRemove -= MyEntities_OnEntityRemove;

            if (MyAPIGateway.Session.IsServer)
            {
                MyVisualScriptLogicProvider.PlayerConnected -= PlayerConnected;
                MyEntities.OnEntityAdd -= Entities_OnEntityAdd;
            }

            DragClientAPI.Close();
        }

        private void Entities_OnEntityCreate(MyEntity entity)
        {
            if (entity.IsPreview || entity.Closed)
                return;

            if(entity is MyPlanet)
            {
                Water water;
                if (Waters != null && Waters.TryGetValue(entity.EntityId, out water))
                {
                    water.Init();
                }
            }

            AddPhysicsComponentToEntity(entity);
        }

        private void AddPhysicsComponentToEntity(MyEntity entity)
        {
            if (entity is IMyCubeGrid && !entity.Components.Has<WaterPhysicsComponentGrid>())
                entity.Components.Add(new WaterPhysicsComponentGrid(_managerComponent));
            else if (entity is IMyFloatingObject && !entity.Components.Has<WaterPhysicsComponentFloatingObject>())
                entity.Components.Add(new WaterPhysicsComponentFloatingObject(_managerComponent));
            else if (entity is IMyCharacter && !entity.Components.Has<WaterPhysicsComponentCharacter>())
                entity.Components.Add(new WaterPhysicsComponentCharacter(_managerComponent));
            else if (entity is IMyInventoryBag && !entity.Components.Has<WaterPhysicsComponentInventoryBag>())
                entity.Components.Add(new WaterPhysicsComponentInventoryBag(_managerComponent));
        }

        /// <summary>
        /// Called once every tick after the simulation has run
        /// </summary>
        public override void UpdateAfterSimulation()
        {
            _tickTimerSecond--;
            if (_tickTimerSecond <= 0)
            {
                UpdateAfter60?.Invoke();
                _tickTimerSecond = 60;
            }

            SimulatePhysics();

            UpdateAfter1?.Invoke();
        }

        /// <summary>
        /// Simulates water physics
        /// </summary>
        public void SimulatePhysics()
        {
            if(Waters != null)
                foreach (var water in Waters.Values)
                {
                    water.Simulate();
                }
        }

        /// <summary>
        /// Player connects to server event
        /// </summary>
        private void PlayerConnected(long playerId)
        {
            if (MyAPIGateway.Session.IsServer)
                SyncClients();
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

        public void SyncToServer()
        {
            if (Waters == null)
                return;

            if (MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Moderator)
            {
                if (MyAPIGateway.Session.IsServer)
                    SyncClients();
                else
                    MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ClientHandlerID, MyAPIGateway.Utilities.SerializeToBinary(new SerializableDictionary<long, Water>(Waters)));
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
                Waters = TempWaters;

                if (MyAPIGateway.Session.IsServer)
                    SyncClients();
            }
        }
    }
}