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

        public void SendSignalToServer(WaterPacket packet)
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
                if(packet is WaterUpdateAddPacket)
                {
                    MyPlanet planet = MyAPIGateway.Entities.GetEntityById(packet.EntityId) as MyPlanet;
                    WaterComponent component;
                    planet.Components.TryGet<WaterComponent>(out component);

                    packet = new WaterUpdateAddPacket
                    {
                        EntityId = packet.EntityId,
                        Settings = (packet as WaterUpdateAddPacket).Settings,
                        Timer = component?.WaveTimer ?? 0,
                    };
                }

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
            var serializedPacket = MyAPIGateway.Utilities.SerializeFromBinary<WaterPacket>(packet);
            if(serializedPacket != null)
            {
                if(MyAPIGateway.Session.GetUserPromoteLevel(sender) >= serializedPacket.RequiredPromotionLevel)
                {
                    ComputeConfirmedPacket(packet);

                    SendSignalToClients(serializedPacket);
                }
            }
        }

        private void Client_OnMessageReceived(ushort channel, byte[] packet, ulong sender, bool isArrivedFromServer)
        {
            if (isArrivedFromServer)
            {
                ComputeConfirmedPacket(packet);
            }
        }

        private void ComputeConfirmedPacket(byte[] packet)
        {
            var serializedPacket = MyAPIGateway.Utilities.SerializeFromBinary<WaterPacket>(packet);
            if (serializedPacket != null)
            {
                MyPlanet planet = MyAPIGateway.Entities.GetEntityById(serializedPacket.EntityId) as MyPlanet;
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
                    SendSignalToClients(new WaterUpdateAddPacket
                    {
                        EntityId = water.Planet.EntityId,
                        Settings = water.Settings,
                        Timer = water.WaveTimer,
                    });
                }
            }
        }

        public override void LoadData()
        {
            _modComponent = Session.Instance.Get<WaterModComponent>();

            if (MyAPIGateway.Session.IsServer)
            {
                MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(WaterData.ServerHandlerID, Server_OnMessageRecieved);
                MyVisualScriptLogicProvider.PlayerConnected += PlayerConnected;
            }

            if(!MyAPIGateway.Utilities.IsDedicated)
            {
                MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(WaterData.ClientHandlerID, Client_OnMessageReceived);
            }
        }

        public override void UnloadData()
        {
            if (MyAPIGateway.Session.IsServer)
            {
                MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(WaterData.ServerHandlerID, Server_OnMessageRecieved);
                MyVisualScriptLogicProvider.PlayerConnected -= PlayerConnected;
            }

            if (!MyAPIGateway.Utilities.IsDedicated)
            {
                MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(WaterData.ClientHandlerID, Client_OnMessageReceived);
            }
        }
    }

    /// <summary>
    /// Base packet for syncing water components
    /// </summary>
    [ProtoInclude(1001, typeof(WaterUpdateAddPacket))]
    [ProtoInclude(1002, typeof(WaterRemovePacket))]
    [ProtoInclude(1003, typeof(WaterSyncPacket))]
    [ProtoContract]
    public abstract class WaterPacket
    {
        /// <summary>
        /// The ID of the entity with the water component
        /// </summary>
        [ProtoMember(1)]
        public long EntityId;

        /// <summary>
        /// The required promotion level the sender requires for the server to accept the packet
        /// </summary>
        [ProtoIgnore]
        public abstract MyPromoteLevel RequiredPromotionLevel { get; }

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

        [ProtoIgnore]
        public override MyPromoteLevel RequiredPromotionLevel => MyPromoteLevel.SpaceMaster;

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

        [ProtoIgnore]
        public override MyPromoteLevel RequiredPromotionLevel => MyPromoteLevel.SpaceMaster;

        public WaterSyncPacket() { }
    }

    /// <summary>
    /// Packet used to remove the water from an entity
    /// </summary>
    [ProtoContract]
    public class WaterRemovePacket : WaterPacket
    {
        [ProtoIgnore]
        public override MyPromoteLevel RequiredPromotionLevel => MyPromoteLevel.SpaceMaster;

        public WaterRemovePacket() { }
    }
}
