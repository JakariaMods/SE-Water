using Jakaria.Utils;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VRage.Game;
using VRage.ObjectBuilders;

namespace Jakaria.Configs
{
    [ProtoContract]
    public class MaterialConfig : BaseConfig
    {
        [ProtoMember(1)]
        public float Density = 1024;

        [ProtoMember(5)]
        public float Viscosity = 0.1f;

        [ProtoMember(10)]
        public bool DrawSplashes = true;

        [ProtoMember(15)]
        public bool DrawBubbles = true;

        [ProtoMember(20), XmlElement("CollectedItem")]
        public string CollectedItemSubtypeId;

        [ProtoIgnore(), XmlIgnore()]
        public MyObjectBuilder_PhysicalObject CollectedItem;

        [ProtoMember(21)]
        public int CollectedAmount = 50;

        public override void Init()
        {
            try
            {
                CollectedItem = new MyObjectBuilder_Ore() { SubtypeName = CollectedItemSubtypeId };
            }
            catch(Exception e)
            {
                CollectedItem = null;
                WaterUtils.WriteLog(e.ToString());
            }

            Viscosity = Math.Min(Viscosity, 1f);

            base.Init();
        }

        public MaterialConfig() { }
    }
}
