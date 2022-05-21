using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jakaria
{
    [ProtoContract]
    public class WaterClientSettings
    {
        [ProtoMember(1)]
        public float Quality = 1.5f;

        [ProtoMember(2)]
        public bool ShowHud = true;

        [ProtoMember(3)]
        public bool ShowCenterOfBuoyancy = false;

        [ProtoMember(4)]
        public bool ShowDepth = true;

        [ProtoMember(5)]
        public bool ShowFog = true;

        [ProtoMember(6)]
        public bool ShowDebug = false;

        [ProtoMember(7)]
        public float Volume = 1f;

        [ProtoMember(8)]
        public bool ShowAltitude = true;
    }
}
