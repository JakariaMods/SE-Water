using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jakaria
{
    public class WaterSettings
    {
        [ProtoMember(5)]
        public float Radius = 1;

        [ProtoMember(10)]
        public float WaveHeight = 0.1f;

        [ProtoMember(11)]
        public float WaveSpeed = 40f;

        [ProtoMember(12)]
        public float Viscosity = 0.1f;

        [ProtoMember(13)]
        public float Buoyancy = 1f;

        [ProtoMember(15)]
        public bool EnableFish = true;

        [ProtoMember(16)]
        public bool EnableSeagulls = true;

        public WaterSettings()
        {

        }

        public WaterSettings(Water water)
        {
            this.Radius = water.radius / water.planet.MinimumRadius;
            this.WaveHeight = water.waveHeight;
            this.WaveSpeed = water.waveSpeed;
            this.Viscosity = water.viscosity;
            this.Buoyancy = water.buoyancy;
            this.EnableFish = water.enableFish;
            this.EnableSeagulls = water.enableSeagulls;
        }
    }
}
