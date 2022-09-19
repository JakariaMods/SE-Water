using ProtoBuf;
using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VRage.Game;
using VRageMath;

namespace Jakaria.Configs
{
    [ProtoContract]
    public class FishConfig : BaseConfig
    {
        [ProtoMember(1)]
        public string ModelPath;

        [ProtoMember(5)]
        public Vector3 AnimationStrength;

        [ProtoMember(6)]
        public float AnimationSpeed;

        [ProtoMember(10)]
        public Vector3 ChildOffset;

        [ProtoMember(15)]
        public float MoveSpeed;

        [ProtoMember(16)]
        public float TurnSpeed;

        [ProtoMember(20)]
        public float AITargetDistance;

        [ProtoMember(25)]
        public float MinimumPressure;

        [ProtoMember(26)]
        public float MaximumPressure;

        [ProtoMember(30)]
        public int SpawnWeight;
    }
}
