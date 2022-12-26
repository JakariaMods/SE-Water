using Jakaria.Components;
using Jakaria.Utils;
using ProtoBuf;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;

namespace Jakaria.SessionComponents
{
    /// <summary>
    /// Session component that handles syncing of water's across clients and server
    /// </summary>
    public class WaterSyncComponent : SessionComponentBase
    {
        private WaterModComponent _modComponent;
        private WaterCommandComponent _commandComponent;

        public event Action<WaterComponent> OnWaterUpdated;

        private int _timer;

        public override void UpdateAfterSimulation()
        {
            _timer++;
            if (_timer > MyEngineConstants.UPDATE_STEPS_PER_MINUTE)
            {
                _timer = 0;

                foreach (var water in _modComponent.Waters)
                {
                    SendSignalToClients(new WaterSyncPacket
                    {
                        EntityId = water.Entity.EntityId,
                        WaveTimer = water.WaveTimer,
                        TideTimer = water.TideTimer,
                    });
                }
            }
        }

        public void SendSignalToServer(Packet packet)
        {
            if (!MyAPIGateway.Utilities.IsDedicated)
            {
                MyAPIGateway.Multiplayer.SendMessageToServer(WaterData.ServerHandlerID, MyAPIGateway.Utilities.SerializeToBinary(packet));
            }
        }

        public void SendSignalToClients(WaterPacket packet)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                byte[] data = MyAPIGateway.Utilities.SerializeToBinary(packet);
                MyAPIGateway.Multiplayer.SendMessageToOthers(WaterData.ClientHandlerID, data);

                if (!MyAPIGateway.Utilities.IsDedicated)
                {
                    Client_OnMessageReceived(WaterData.ClientHandlerID, data, 0, true);
                }
            }
        }

        private void SendSignalToClient(WaterPacket packet, ulong recipient)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                byte[] data = MyAPIGateway.Utilities.SerializeToBinary(packet);
                MyAPIGateway.Multiplayer.SendMessageTo(WaterData.ClientHandlerID, data, recipient);
            }
        }

        private void Server_OnMessageRecieved(ushort channel, byte[] packet, ulong sender, bool isArrivedFromServer)
        {
            try
            {
                var serializedPacket = MyAPIGateway.Utilities.SerializeFromBinary<Packet>(packet);
                if (serializedPacket != null)
                {
                    if(serializedPacket is CommandPacket)
                    {
                        ComputeConfirmedPacket(packet, sender);
                    }
                }
            }
            catch (Exception e)
            {
                WaterUtils.WriteLog($"Received malformed packet from {sender}");
                WaterUtils.WriteLog(e.ToString());
            }
        }

        private void Client_OnMessageReceived(ushort channel, byte[] packet, ulong sender, bool isArrivedFromServer)
        {
            if (isArrivedFromServer)
            {
                ComputeConfirmedPacket(packet, sender);
            }
        }

        private void ComputeConfirmedPacket(byte[] packet, ulong sender)
        {
            var serializedPacket = MyAPIGateway.Utilities.SerializeFromBinary<Packet>(packet);
            if (serializedPacket != null)
            {
                CommandPacket commandPacket = serializedPacket as CommandPacket;
                if(commandPacket != null)
                {
                    _commandComponent.SendCommand(commandPacket.Message, sender);
                }

                WaterPacket waterPacket = serializedPacket as WaterPacket;
                if(waterPacket != null)
                {
                    MyPlanet planet = MyAPIGateway.Entities.GetEntityById(waterPacket.EntityId) as MyPlanet;
                    if (planet != null)
                    {
                        if (serializedPacket is WaterRemovePacket)
                        {
                            _modComponent.RemoveWater(planet);
                        }
                        else if (serializedPacket is WaterUpdateAddPacket)
                        {
                            WaterSettings settings = ((WaterUpdateAddPacket)serializedPacket).Settings;
                            double timer = ((WaterUpdateAddPacket)serializedPacket).Timer;
                            WaterComponent waterComponent;
                            if (planet.Components.TryGet<WaterComponent>(out waterComponent))
                            {
                                Assert.NotNull(waterComponent.Settings);

                                waterComponent.Settings = settings;
                                waterComponent.WaveTimer = timer;

                                OnWaterUpdated?.Invoke(waterComponent);
                            }
                            else
                            {
                                _modComponent.AddWater(planet, settings);
                            }
                        }
                        else if (serializedPacket is WaterSyncPacket)
                        {
                            var syncPacket = (WaterSyncPacket)serializedPacket;
                            var water = _modComponent.GetWaterById(syncPacket.EntityId);

                            if (water != null)
                            {
                                water.WaveTimer = syncPacket.WaveTimer;
                                water.TideTimer = syncPacket.TideTimer;
                            }
                        }
                    }
                }
            }
        }

        private void PlayerConnected(long playerId)
        {
            SyncClients();
        }

        public void SyncClients()
        {
            if (MyAPIGateway.Session.IsServer)
            {
                foreach (var water in _modComponent.Waters)
                {
                    SyncClients(water);
                }
            }
        }

        public void SyncClients(WaterComponent water)
        {
            Assert.True(MyAPIGateway.Session.IsServer, "Sync Clients should never be called from client!");

            SendSignalToClients(new WaterUpdateAddPacket
            {
                EntityId = water.Planet.EntityId,
                Settings = water.Settings,
                Timer = water.WaveTimer,
            });
        }

        public override void LoadData()
        {
            _modComponent = Session.Instance.Get<WaterModComponent>();
            _commandComponent = Session.Instance.Get<WaterCommandComponent>();

            if (MyAPIGateway.Session.IsServer)
            {
                MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(WaterData.ServerHandlerID, Server_OnMessageRecieved);
                MyVisualScriptLogicProvider.PlayerConnected += PlayerConnected;
                _modComponent.OnWaterRemoved += OnWaterRemoved;
                _modComponent.OnWaterAdded += OnWaterAdded;
            }

            if(!MyAPIGateway.Utilities.IsDedicated)
            {
                MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(WaterData.ClientHandlerID, Client_OnMessageReceived);
            }
        }

        private void OnWaterAdded(MyEntity entity)
        {
            SyncClients(entity.Components.Get<WaterComponent>());
        }

        private void OnWaterRemoved(MyEntity entity)
        {
            SendSignalToClients(new WaterRemovePacket
            {
                EntityId = entity.EntityId,
            });
        }

        public override void UnloadData()
        {
            if (MyAPIGateway.Session.IsServer)
            {
                MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(WaterData.ServerHandlerID, Server_OnMessageRecieved);
                MyVisualScriptLogicProvider.PlayerConnected -= PlayerConnected;
                _modComponent.OnWaterAdded -= OnWaterAdded;
                _modComponent.OnWaterRemoved -= OnWaterRemoved;
            }

            if (!MyAPIGateway.Utilities.IsDedicated)
            {
                MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(WaterData.ClientHandlerID, Client_OnMessageReceived);
            }
        }
    }

    /*[ProtoInclude(1001, typeof(WaterUpdateAddPacket))]
    [ProtoInclude(1002, typeof(WaterRemovePacket))]
    [ProtoInclude(1003, typeof(WaterSyncPacket))]*/
    [ProtoInclude(1004, typeof(WaterPacket))]
    [ProtoInclude(1005, typeof(CommandPacket))]
    [ProtoContract]
    public abstract class Packet
    {
        public Packet() { }
    }

    /// <summary>
    /// Base packet for syncing water components
    /// </summary>
    [ProtoInclude(1001, typeof(WaterUpdateAddPacket))]
    [ProtoInclude(1002, typeof(WaterRemovePacket))]
    [ProtoInclude(1003, typeof(WaterSyncPacket))]
    [ProtoContract]
    public abstract class WaterPacket : Packet
    {
        /// <summary>
        /// The ID of the entity with the water component
        /// </summary>
        [ProtoMember(1)]
        public long EntityId;

        public WaterPacket() { }
    }

    /// <summary>
    /// Packet used to update the water settings of an entity
    /// </summary>
    [ProtoContract]
    public class WaterUpdateAddPacket : WaterPacket
    {
        [ProtoMember(5)]
        public WaterSettings Settings;

        [ProtoMember(10)]
        public double Timer;

        public WaterUpdateAddPacket() { }
    }

    /// <summary>
    /// Packet used to sync timers with server
    /// </summary>
    [ProtoContract]
    public class WaterSyncPacket : WaterPacket
    {
        [ProtoMember(5)]
        public double WaveTimer;

        [ProtoMember(10)]
        public double TideTimer;

        public WaterSyncPacket() { }
    }

    /// <summary>
    /// Packet used to remove the water from an entity
    /// </summary>
    [ProtoContract]
    public class WaterRemovePacket : WaterPacket
    {
        public WaterRemovePacket() { }
    }

    /// <summary>
    /// Packet for sending command messages to server
    /// </summary>
    [ProtoContract]
    public class CommandPacket : Packet
    {
        [ProtoMember(5)]
        public string Message;

        public CommandPacket() { }
    }
}
