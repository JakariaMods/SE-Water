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
        public float Radius = 1;

        [ProtoMember(10)]
        public float WaveHeight = 1f;

        [ProtoMember(11)]
        public float WaveSpeed = 0.04f;

        [ProtoMember(12)]
        public float Viscosity = 0.1f;

        [ProtoMember(13)]
        public float Buoyancy = 1f;

        [ProtoMember(14)]
        public float WaveScale = 3f;

        [ProtoMember(15)]
        public bool EnableFish = true;

        [ProtoMember(16)]
        public bool EnableSeagulls = true;

        [ProtoMember(17)]
        public string Texture = "JWater";

        [ProtoMember(20)]
        public int CrushDepth = 500;

        [ProtoMember(25)]
        public bool Transparent = true;

        [ProtoMember(30)]
        public bool Lit = true;

        [ProtoMember(35)]
        public Vector3D FogColor = new Vector3D(0.1, 0.125, 0.2);

        [ProtoMember(40)]
        public float CollectionRate = 1;

        [ProtoMember(45)]
        public float TideHeight = 2f;

        [ProtoMember(45)]
        public float TideSpeed = 0.25f;

        [ProtoMember(50)]
        public bool EnableFoam = true;

        [ProtoMember(55)]
        public float FluidDensity = 1000;

        public WaterSettings()
        {

        }

        public WaterSettings(Water water)
        {
            this.Radius = water.radius / water.planet.MinimumRadius;
            this.WaveHeight = water.waveHeight;
            this.WaveSpeed = water.waveSpeed;
            this.WaveScale = water.waveScale;
            this.Viscosity = water.viscosity;
            this.Buoyancy = water.buoyancy;
            this.EnableFish = water.enableFish;
            this.EnableSeagulls = water.enableSeagulls;
            this.Texture = water.texture;
            this.CrushDepth = water.crushDepth;
            this.Transparent = water.transparent;
            this.Lit = water.lit;
            this.FogColor = water.fogColor;
            this.CollectionRate = water.collectionRate;
            this.TideHeight = water.tideHeight;
            this.TideSpeed = water.tideSpeed;
            this.EnableFoam = water.enableFoam;
        }
    }
}
