using Jakaria.Utils;
using ProtoBuf;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VRage.Game.Entity;
using VRage.Serialization;
using VRage.Utils;
using VRageMath;

namespace Jakaria.SessionComponents
{
    /// <summary>
    /// Component for backwards compatibility of older saves
    /// </summary>
    public class BackwardsCompatibilityComponent : SessionComponentBase
    {
        private WaterSyncComponent _waterSyncComponent;

        private void MyEntities_OnEntityCreate(MyEntity entity)
        {
            //Old API method for getting water settings on a modded planet
            MyPlanet planet = entity as MyPlanet;
            if(planet != null && planet.Generator != null && planet.Generator.WeatherGenerators != null)
            {
                foreach (var weather in planet.Generator.WeatherGenerators)
                {
                    if (weather.Voxel == "WATERMODDATA")
                    {
                        WaterSettings settings = MyAPIGateway.Utilities.SerializeFromXML<WaterSettings>(weather.Weathers?[0]?.Name);

                        if(settings != null)
                        {
                            _waterSyncComponent.SendSignalToClients(new WaterUpdateAddPacket
                            {
                                EntityId = planet.EntityId,
                                Settings = settings,
                            });
                        }
                    }
                }
            }
        }

        public override void BeforeStart()
        {
            //Old Save formats
            string packet;
            if (MyAPIGateway.Utilities.GetVariable("JWater", out packet) && packet != null)
            {
                //Water Mod 2-3.9 save format
                if (packet.Contains("Dictionary"))
                {
                    SerializableDictionary<long, Water> tempDictWaters = MyAPIGateway.Utilities.SerializeFromXML<SerializableDictionary<long, Water>>(packet);

                    if (tempDictWaters != null)
                    {
                        foreach (var water in tempDictWaters.Dictionary)
                        {
                            _waterSyncComponent.SendSignalToClients(new WaterUpdateAddPacket
                            {
                                EntityId = water.Key,
                                Settings = water.Value.Export(),
                            });
                        }
                    }
                }
                else
                {
                    //Water Mod 1 save format
                    List<Water> tempWaters = MyAPIGateway.Utilities.SerializeFromXML<List<Water>>(packet);

                    if (tempWaters != null)
                    {
                        foreach (var water in tempWaters)
                        {
                            _waterSyncComponent.SendSignalToClients(new WaterUpdateAddPacket
                            {
                                EntityId = water.PlanetID,
                                Settings = water.Export(),
                            });
                        }
                    }
                }

                MyAPIGateway.Utilities.RemoveVariable("JWater");
            }
        }

        public override void LoadData()
        {
            _waterSyncComponent = Session.Instance.Get<WaterSyncComponent>();

            MyEntities.OnEntityCreate += MyEntities_OnEntityCreate;
        }

        public override void UnloadData()
        {
            MyEntities.OnEntityCreate -= MyEntities_OnEntityCreate;
        }

        /// <summary>
        /// Obsolete, stripped version of water object to allow backwards-compatible serialization. Use <see cref="WaterComponent"/> instead
        /// </summary>
        [ProtoContract(UseProtoMembersOnly = true)]
        public class Water
        {
            [ProtoMember(5), XmlElement("planetID")]
            public long PlanetID;

            [ProtoMember(10), XmlElement("radius")]
            public float Radius = WaterSettings.Default.Radius;

            [ProtoMember(15), XmlElement("waveHeight")]
            public float WaveHeight = WaterSettings.Default.WaveHeight;

            [ProtoMember(16), XmlElement("waveSpeed")]
            public float WaveSpeed = WaterSettings.Default.WaveSpeed;

            [ProtoMember(17), XmlElement("waveTimer")]
            public double WaveTimer = 0;

            [ProtoMember(18), XmlElement("waveScale")]
            public float WaveScale = WaterSettings.Default.WaveScale;

            [ProtoMember(20), XmlElement("position")]
            public Vector3D Position;

            [ProtoMember(26), XmlElement("buoyancy")]
            public float Buoyancy = WaterSettings.Default.Buoyancy;

            [ProtoMember(30), XmlElement("enableFish")]
            public bool EnableFish = WaterSettings.Default.EnableFish;

            [ProtoMember(31), XmlElement("enableSeagulls")]
            public bool EnableSeagulls = WaterSettings.Default.EnableSeagulls;

            [ProtoMember(33), XmlElement("enableFoam")]
            public bool EnableFoam = WaterSettings.Default.EnableFoam;

            [ProtoMember(32), XmlElement("texture")]
            public MyStringId Texture = WaterSettings.Default.Texture;

            [ProtoMember(35), XmlElement("crushDamage")]
            public float CrushDamage = WaterSettings.Default.CrushDamage;

            [ProtoMember(40), XmlElement("playerDrag")]
            public bool PlayerDrag = WaterSettings.Default.PlayerDrag;

            [ProtoMember(45), XmlElement("transparent")]
            public bool Transparent = WaterSettings.Default.Transparent;

            [ProtoMember(50), XmlElement("lit")]
            public bool Lit = WaterSettings.Default.Lit;

            [ProtoMember(55), XmlElement("collectionRate")]
            public float CollectionRate = WaterSettings.Default.CollectionRate;

            [ProtoMember(60), XmlElement("fogColor")]
            public Vector3 FogColor = WaterSettings.Default.FogColor;

            [ProtoMember(65), XmlElement("tideHeight")]
            public float TideHeight = WaterSettings.Default.TideHeight;

            [ProtoMember(66), XmlElement("tideSpeed")]
            public float TideSpeed = WaterSettings.Default.TideSpeed;

            [ProtoMember(67), XmlElement("tideTimer")]
            public double TideTimer = 0;

            [ProtoMember(70), XmlElement("material")]
            public string MaterialId;

            [ProtoMember(75), XmlElement("currentSpeed")]
            public float CurrentSpeed = WaterSettings.Default.CurrentSpeed;

            [ProtoMember(76), XmlElement("currentScale")]
            public float CurrentScale = WaterSettings.Default.CurrentScale;

            public Water() { }

            public WaterSettings Export()
            {
                MyPlanet planet = MyEntities.GetEntityById(PlanetID) as MyPlanet;

                WaterSettings settings = new WaterSettings();

                if(planet != null)
                    settings.Radius = Radius / planet.MinimumRadius;

                settings.WaveHeight = WaveHeight;
                settings.WaveSpeed = WaveSpeed;
                settings.WaveScale = WaveScale;
                settings.Buoyancy = Buoyancy;
                settings.EnableFish = EnableFish;
                settings.EnableSeagulls = EnableSeagulls;
                settings.Texture = SerializableStringId.Create(Texture);
                settings.CrushDamage = CrushDamage;
                settings.Transparent = Transparent;
                settings.Lit = Lit;
                settings.FogColor = FogColor;
                settings.CollectionRate = CollectionRate;
                settings.TideHeight = TideHeight;
                settings.TideSpeed = TideSpeed;
                settings.EnableFoam = EnableFoam;

                return settings;
            }
        }
    }
}
