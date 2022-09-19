using Jakaria.Utils;
using ProtoBuf;
using System;
using System.Xml.Serialization;
using VRage.Game;
using VRage.Game.ModAPI;

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
        public string CollectedItemSubtypeId
        {
            get { return _collectedItemSubtypeId; }
            set
            {
                _collectedItemSubtypeId = value;
                CollectedItem = new MyObjectBuilder_Ore() { SubtypeName = _collectedItemSubtypeId };

                WaterUtils.WriteLog($"Identified CollectedItem '{_collectedItemSubtypeId}' Null? '{CollectedItem == null}'");
            }
        }

        private string _collectedItemSubtypeId = "Ice";

        [ProtoIgnore(), XmlIgnore()]
        public MyObjectBuilder_PhysicalObject CollectedItem;

        [ProtoMember(26)]
        public int CollectedAmount = 10;

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

        [ProtoMember(45)]
        public float Fresnel = 1;

        public override void Init(IMyModContext modContext = null)
        {
            CollectedItemSubtypeId = _collectedItemSubtypeId;
            Viscosity = Math.Min(Viscosity, 1f);

            base.Init(modContext);
        }

        public MaterialConfig() { }

        public MaterialConfig(bool AutoInit) { if (AutoInit) base.Init(null); }
    }
}
