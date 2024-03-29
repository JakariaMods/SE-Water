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
    public class WeatherConfig : BaseConfig
    {
        [ProtoMember(1)]
        public float WaveHeightMultiplier = 1;

        [ProtoMember(5)]
        public float WaveScaleMultiplier = 1;

        [ProtoMember(10)]
        public float WaveSpeedMultiplier = 1;
    }
}
