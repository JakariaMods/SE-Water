using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Jakaria.Configs
{
    [ProtoContract]
    public class RespawnPodConfig : BaseConfig
    {
        [ProtoMember(1), XmlAttribute("WaterSpawnAltitude")]
        public float WaterSpawnAltitude = 1000;

        [ProtoMember(5), XmlAttribute("SpawnOnWater")]
        public bool SpawnOnWater = false;

        public RespawnPodConfig() { }
    }
}
