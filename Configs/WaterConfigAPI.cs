using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Jakaria.Configs
{
    [ProtoContract, XmlRoot("WaterConfig")]
    public class WaterConfigAPI
    {
        [ProtoMember(1)]
        public BlockConfig[] BlockConfigs;

        [ProtoMember(5)]
        public PlanetConfig[] PlanetConfigs;

        [ProtoMember(10)]
        public CharacterConfig[] CharacterConfigs;

        [ProtoMember(15)]
        public RespawnPodConfig[] RespawnPodConfigs;

        [ProtoMember(20), XmlArrayItem("Texture")]
        public string[] WaterTextures;

        [ProtoMember(25)]
        public MaterialConfig[] MaterialConfigs;

        [ProtoMember(30)]
        public FishConfig[] FishConfigs;

        [ProtoMember(35)]
        public WeatherConfig[] WeatherConfigs;
    }
}
