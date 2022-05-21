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
        [ProtoMember(1)]
        public float SpawnAltitude = 1000;

        [ProtoMember(5)]
        public bool SpawnOnWater = false;

        [ProtoMember(10)]
        public bool SpawnOnLand = true;

        public RespawnPodConfig() { }
    }
}
