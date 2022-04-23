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
    public class BaseConfig
    {
        [XmlAttribute("TypeId")]
        public string TypeId = "(null)";
        [XmlAttribute("SubtypeId")]
        public string SubtypeId = "(null)";

        [ProtoIgnore, XmlIgnore]
        public MyDefinitionId DefinitionId;

        public virtual void Init()
        {
            MyDefinitionId.TryParse(TypeId + "/" + SubtypeId, out DefinitionId);
        }

        public BaseConfig(string typeId, string subtypeId)
        {
            TypeId = typeId;
            SubtypeId = subtypeId;
        }

        public BaseConfig() { }
    }
}
//TODO RENAME CONFIG TO DEFINITION TO MATCH VANILLA PATTERN