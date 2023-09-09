using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VRage.Serialization;
using VRage.Utils;

namespace Jakaria.Utils
{
    /// <summary>
    /// Version of <see cref="MyStringId"/> that can be serialized to binary
    /// </summary>
    [ProtoContract, Serializable]
    public struct SerializableStringId
    {
        [ProtoIgnore, XmlIgnore]
        public MyStringId Value;

        [ProtoMember(1), XmlElement]
        public string String
        {
            get
            {
                return Value.String;
            }
            set
            {
                Value = MyStringId.GetOrCompute(value);
            }
        }

        public static SerializableStringId Create(MyStringId value)
        {
            return new SerializableStringId
            {
                Value = value,
            };
        }

        public static implicit operator MyStringId(SerializableStringId value) => value.Value;

        public static bool operator ==(SerializableStringId a, SerializableStringId b)
        {
            return a.Value == b.Value;
        }

        public static bool operator !=(SerializableStringId a, SerializableStringId b)
        {
            return a.Value != b.Value;
        }

        public override string ToString()
        {
            return String;
        }
    }
}
