using ProtoBuf;
using System;
using System.Xml.Serialization;
using VRage.Game;

namespace Jakaria.Configs
{
    [ProtoContract]
    public class MaterialConfig : BaseConfig
    {
        [ProtoMember(1)]
        public float Density = 1024;

        [ProtoMember(5)]
        public float Viscosity = 0.05f;

        [ProtoMember(10)]
        public bool DrawSplashes = true;

        [ProtoMember(15)]
        public bool DrawBubbles = true;

        [ProtoMember(20)]
        public bool DrawWakes = true;

        [ProtoMember(25), XmlElement("CollectedItem")]
        public string CollectedItemSubtypeId = "Ice";

        [ProtoIgnore(), XmlIgnore()]
        public MyObjectBuilder_PhysicalObject CollectedItem;

        [ProtoMember(26)]
        public int CollectedAmount = 50;

        [ProtoMember(30)]
        public float MaxSurfaceFluctuation = 5f;

        [ProtoMember(31)]
        public float SurfaceFluctuationAngleSpeed = 2f;

        [ProtoMember(32)]
        public float SurfaceFluctuationSpeed = 0.5f;

        [ProtoMember(35)]
        public float Reflectivity = 0.7f;

        [ProtoMember(40)]
        public float UnderwaterReflectivity = 0.2f;

        public override void Init()
        {
            CollectedItem = new MyObjectBuilder_Ore() { SubtypeName = CollectedItemSubtypeId };
            Viscosity = Math.Min(Viscosity, 1f);

            base.Init();
        }

        public MaterialConfig() { }

        public MaterialConfig(bool AutoInit) { if (AutoInit) base.Init(); }
    }
}
