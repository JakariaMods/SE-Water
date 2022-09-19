using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VRage.Game;

namespace Jakaria.Configs
{
    [ProtoContract]
    public class PlanetConfig : BaseConfig
    {
        [ProtoMember(1)]
        public WaterSettings WaterSettings;

        [ProtoMember(5)]
        public float ColorIntensity = 1;

        [ProtoMember(10)]
        public float Specularity = 4;

        [ProtoMember(11)]
        public float SpecularIntensity = 1;

        [ProtoMember(15)]
        public float AmbientColorIntensity = 0.2f;

        [ProtoMember(20)]
        public float DistantRadiusOffset = 0.021f;

        [ProtoMember(21)]
        public float DistantRadiusScaler = 2f;

        [ProtoMember(25)]
        public float DistantSoftnessOffset = 0.003f;

        [ProtoMember(26)]
        public float DistantSoftnessScaler = 2f;

        [ProtoMember(27)]
        public float DistantSoftnessMultiplier = 5f;

        public PlanetConfig(MyDefinitionId definitionId)
        {
            TypeId = definitionId.TypeId.ToString();
            SubtypeId = definitionId.SubtypeName;
            DefinitionId = definitionId;
        }

        public PlanetConfig() { }
    }
}
