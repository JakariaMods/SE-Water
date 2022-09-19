using ProtoBuf;
using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VRage.Game;
using VRage.Game.ModAPI;

namespace Jakaria.Configs
{
    [ProtoContract]
    public class BlockConfig : BaseConfig
    {
        [ProtoMember(1), XmlAttribute("Volume")]
        public float Volume = -1;

        [ProtoMember(5), XmlAttribute("FunctionalUnderwater")]
        public bool FunctionalUnderWater = true;

        [ProtoMember(6), XmlAttribute("FunctionalAboveWater")]
        public bool FunctionalAboveWater = true;

        [ProtoMember(10), XmlAttribute("MaximumPressure")]
        public float MaximumPressure = -1;

        [ProtoMember(11), XmlAttribute("MaxFunctionalPressure")]
        public float MaxFunctionalPressure = -1;

        [ProtoMember(15), XmlAttribute("PlayDamageEffect")]
        public bool PlayDamageEffect = false;

        [ProtoMember(20), XmlAttribute("IsPressurized")]
        public bool IsPressurized = false;

        public override void Init(IMyModContext modContext = null)
        {
            if(modContext != null)
                base.Init(modContext);

            /*if (Volume < 0)
                Volume = 0;*/

            if (MaximumPressure < 0)
                MaximumPressure = float.MaxValue;

            if (MaxFunctionalPressure < 0)
                MaxFunctionalPressure = float.MaxValue;
        }
    }
}
