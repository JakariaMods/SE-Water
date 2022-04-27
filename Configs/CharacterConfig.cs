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
    public class CharacterConfig : BaseConfig
    {
        [ProtoMember(1), XmlAttribute("Volume")]
        public float Volume = -1;

        [ProtoMember(5), XmlAttribute("SwimForce")]
        public float SwimForce = 1000;

        [ProtoMember(10), XmlAttribute("CanBreathUnderwater")]
        public bool CanBreathUnderwater = false;

        [ProtoMember(15), XmlAttribute("MaximumPressure")]
        public float MaximumPressure = -1;

        [ProtoMember(20), XmlAttribute("Breath")]
        public int Breath = 10;

        [ProtoMember(25), XmlAttribute("DrowningDamage")]
        public int DrowningDamage = 15;

        public CharacterConfig(string typeId, string subtypeId)
        {
            TypeId = typeId;
            SubtypeId = subtypeId;
        }

        public override void Init()
        {
            if (MaximumPressure == -1)
                MaximumPressure = float.MaxValue;

            base.Init();
        }

        public CharacterConfig() { }
    }
}
