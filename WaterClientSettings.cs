using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jakaria
{
    [ProtoContract]
    public struct WaterClientSettings
    {
        [ProtoMember(1)]
        public float Quality;

        [ProtoMember(10)]
        public bool ShowCenterOfBuoyancy;

        [ProtoMember(15)]
        public bool ShowDepth;

        [ProtoIgnore()]
        public bool ShowFog;

        [ProtoMember(25)]
        public bool ShowDebug;

        [ProtoMember(30)]
        public float Volume;

        [ProtoMember(35)]
        public bool ShowAltitude;

        public static WaterClientSettings Default = new WaterClientSettings()
        {
            Quality = 1.5f,
            ShowCenterOfBuoyancy = false,
            ShowDepth = true,
            ShowFog = true,
            ShowDebug = false,
            Volume = 1f,
            ShowAltitude = true,
        };
    }
}
