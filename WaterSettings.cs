using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Utils;
using Jakaria.API;
using VRageMath;

namespace Jakaria
{
    public class WaterSettings
    {
        [ProtoMember(5)]
        public float Radius = Default.Radius;

        [ProtoMember(10)]
        public float WaveHeight = Default.WaveHeight;

        [ProtoMember(11)]
        public float WaveSpeed = Default.WaveSpeed;

        [ProtoMember(13)]
        public float Buoyancy = Default.Buoyancy;

        [ProtoMember(14)]
        public float WaveScale = Default.WaveScale;

        [ProtoMember(15)]
        public bool EnableFish = Default.EnableFish;

        [ProtoMember(16)]
        public bool EnableSeagulls = Default.EnableSeagulls;

        [ProtoMember(17)]
        public string Texture = Default.Texture;

        [ProtoMember(25)]
        public bool Transparent = Default.Transparent;

        [ProtoMember(30)]
        public bool Lit = Default.Lit;

        [ProtoMember(35)]
        public Vector3 FogColor = Default.FogColor;

        [ProtoMember(40)]
        public float CollectionRate = Default.CollectionRate;

        [ProtoMember(45)]
        public float TideHeight = Default.TideHeight;

        [ProtoMember(45)]
        public float TideSpeed = Default.TideSpeed;

        [ProtoMember(50)]
        public bool EnableFoam = Default.EnableFoam;

        [ProtoMember(60)]
        public float CrushDamage = Default.CrushDamage;

        [ProtoMember(65)]
        public bool PlayerDrag = Default.PlayerDrag;

        [ProtoMember(70)]
        public string Material = Default.Material;

        [ProtoMember(75)]
        public float CurrentSpeed = Default.CurrentSpeed;

        [ProtoMember(76)]
        public float CurrentScale = Default.CurrentScale;

        public WaterSettings()
        {

        }

        public WaterSettings(Water water)
        {
            this.Radius = water.Radius / water.Planet.MinimumRadius;
            this.WaveHeight = water.WaveHeight;
            this.WaveSpeed = water.WaveSpeed;
            this.WaveScale = water.WaveScale;
            this.Buoyancy = water.Buoyancy;
            this.EnableFish = water.EnableFish;
            this.EnableSeagulls = water.EnableSeagulls;
            this.Texture = water.Texture;
            this.Transparent = water.Transparent;
            this.Lit = water.Lit;
            this.FogColor = water.FogColor;
            this.CollectionRate = water.CollectionRate;
            this.TideHeight = water.TideHeight;
            this.TideSpeed = water.TideSpeed;
            this.EnableFoam = water.EnableFoam;
            this.CrushDamage = water.CrushDamage;
            this.PlayerDrag = water.PlayerDrag;
            this.Material = water.MaterialId;
            this.CurrentSpeed = water.CurrentSpeed;
        }

        public class Default
        {
            public const float Radius = 1;
            public const float WaveHeight = 1f;
            public const float WaveSpeed = 0.04f;
            public const float Buoyancy = 1f;
            public const float WaveScale = 3f;
            public const bool EnableFish = true;
            public const bool EnableSeagulls = true;
            public const string Texture = "JWater";
            public const bool Transparent = true;
            public const bool Lit = true;
            public static readonly Vector3 FogColor = new Vector3(0.05f, 0.18f, 0.25f);
            public const float CollectionRate = 1;
            public const float TideHeight = 2f;
            public const float TideSpeed = 0.05f;
            public const bool EnableFoam = true;
            public const float CrushDamage = 0.5f;
            public const bool PlayerDrag = true;
            public const string Material = "Water";
            public const float CurrentSpeed = 0.5f;
            public const float CurrentScale = 0.005f;
        };
    }
}
