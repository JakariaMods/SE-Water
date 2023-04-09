using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Utils;
using Jakaria.API;
using VRageMath;
using System.Xml.Serialization;
using Jakaria.Configs;
using Jakaria.Utils;

namespace Jakaria
{
    [ProtoContract]
    public class WaterSettings
    {
        [ProtoMember(5), XmlElement]
        public float Radius;

        [ProtoMember(10), XmlElement]
        public float WaveHeight;

        [ProtoMember(11), XmlElement]
        public float WaveSpeed;

        [ProtoMember(13), XmlElement]
        public float Buoyancy;

        [ProtoMember(14), XmlElement]
        public float WaveScale;

        [ProtoMember(15), XmlElement]
        public bool EnableFish;

        [ProtoMember(16), XmlElement]
        public bool EnableSeagulls;

        [ProtoMember(17), XmlElement]
        public SerializableStringId Texture;

        [ProtoMember(25), XmlElement]
        public bool Transparent;

        [ProtoMember(30), XmlElement]
        public bool Lit;

        [ProtoMember(35), XmlElement]
        public Vector3 FogColor;

        [ProtoMember(40), XmlElement]
        public float CollectionRate;

        [ProtoMember(45), XmlElement]
        public float TideHeight;

        [ProtoMember(46), XmlElement]
        public float TideSpeed;

        [ProtoMember(50), XmlElement]
        public bool EnableFoam;

        [ProtoMember(60), XmlElement]
        public float CrushDamage;

        [ProtoMember(65), XmlElement]
        public bool PlayerDrag;

        [ProtoMember(70), XmlElement]
        public string MaterialId
        {
            get
            {
                return _material;
            }
            set
            {
                if(_material != value)
                {
                    _material = value;

                    if ((MaterialId != null && !WaterData.MaterialConfigs.TryGetValue(MaterialId, out Material)) || Material == null)
                        Material = new MaterialConfig();
                }
            }
        }

        private string _material;

        [ProtoIgnore, XmlIgnore]
        public MaterialConfig Material;

        [ProtoMember(75), XmlElement]
        public float CurrentSpeed;

        [ProtoMember(76), XmlElement]
        public float CurrentScale;

        [ProtoIgnore]//[ProtoMember(80), XmlElement]
        public bool Volumetric;

        public WaterSettings()
        {
            if(Default != null)
            {
                Radius = Default.Radius;
                WaveHeight = Session.CONSOLE_MODE ? 0 : Default.WaveHeight;
                WaveSpeed = Session.CONSOLE_MODE ? 0 : Default.WaveSpeed;
                Buoyancy = Default.Buoyancy;
                WaveScale = Session.CONSOLE_MODE ? 0 : Default.WaveScale;
                EnableFish = Default.EnableFish;
                EnableSeagulls = Default.EnableSeagulls;
                Texture = Default.Texture;
                Transparent = Default.Transparent;
                Lit = Default.Lit;
                FogColor = Default.FogColor;
                CollectionRate = Default.CollectionRate;
                TideHeight = Default.TideHeight;
                TideSpeed = Default.TideSpeed;
                EnableFoam = Default.EnableFoam;
                CrushDamage = Default.CrushDamage;
                PlayerDrag = Default.PlayerDrag;
                MaterialId = Default.MaterialId;
                CurrentSpeed = Default.CurrentSpeed;
                CurrentScale = Default.CurrentScale;
                Volumetric =  Default.Volumetric;
            }

            Init();
        }

        public void Init()
        {
            if ((MaterialId != null && !WaterData.MaterialConfigs.TryGetValue(MaterialId, out Material)) || Material == null)
                Material = new MaterialConfig();
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"{nameof(Radius)}: {Radius}");
            if (!Session.CONSOLE_MODE)
            {
                stringBuilder.AppendLine($"{nameof(WaveHeight)}: {WaveHeight}");
                stringBuilder.AppendLine($"{nameof(WaveSpeed)}: {WaveSpeed}");
                stringBuilder.AppendLine($"{nameof(WaveScale)}: {WaveScale}");
                stringBuilder.AppendLine($"{nameof(EnableFoam)}: {EnableFoam}");
                stringBuilder.AppendLine($"{nameof(EnableFish)}: {EnableFish}");
                stringBuilder.AppendLine($"{nameof(EnableSeagulls)}: {EnableSeagulls}");
                stringBuilder.AppendLine($"{nameof(Texture)}: {Texture}");
                stringBuilder.AppendLine($"{nameof(Transparent)}: {Transparent}");
                stringBuilder.AppendLine($"{nameof(Lit)}: {Lit}");
                stringBuilder.AppendLine($"{nameof(FogColor)}: {FogColor}");
                //stringBuilder.AppendLine($"{nameof(Volumetric)}: {Volumetric}");
            }
            
            stringBuilder.AppendLine($"{nameof(Buoyancy)}: {Buoyancy}");
            stringBuilder.AppendLine($"{nameof(CollectionRate)}: {CollectionRate}");
            stringBuilder.AppendLine($"{nameof(TideHeight)}: {TideHeight}");
            stringBuilder.AppendLine($"{nameof(TideSpeed)}: {TideSpeed}");
            stringBuilder.AppendLine($"{nameof(CrushDamage)}: {CrushDamage}");
            stringBuilder.AppendLine($"{nameof(PlayerDrag)}: {PlayerDrag}");
            stringBuilder.AppendLine($"{nameof(MaterialId)}: {MaterialId}");
            stringBuilder.AppendLine($"{nameof(CurrentSpeed)}: {CurrentSpeed}");
            stringBuilder.AppendLine($"{nameof(CurrentScale)}: {CurrentScale}");

            return stringBuilder.ToString();
        }

        public static readonly WaterSettings Default = new WaterSettings()
        {
            Radius = 1f,
            WaveHeight = 1f,
            WaveSpeed = 0.04f,
            Buoyancy = 1f,
            WaveScale = 3f,
            EnableFish = true,
            EnableSeagulls = true,
            Texture = SerializableStringId.Create(MyStringId.GetOrCompute("JWater")),
            Transparent = true,
            Lit = true,
            FogColor = new Vector3(0.05f, 0.18f, 0.25f),
            CollectionRate = 1f,
            TideHeight = 2f,
            TideSpeed = 0.05f,
            EnableFoam = true,
            CrushDamage = 0.5f,
            PlayerDrag = true,
            MaterialId = "Water",
            CurrentSpeed = 0.5f,
            CurrentScale = 0.005f,
            Volumetric = false,
        };
    }
}
