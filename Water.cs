using Jakaria.API;
using ProtoBuf;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.Models;
using VRage.Game.VisualScripting;
using VRage.Utils;
using VRageMath;
using Jakaria.Configs;
using Jakaria.Utils;
using Sandbox.ModAPI;
using VRage.Game;

namespace Jakaria
{

    [ProtoContract(UseProtoMembersOnly = true)]
    public class Water
    {
        /// <summary>the entity ID of a planet</summary>
        [ProtoMember(5), XmlElement("planetID")]
        public long PlanetID;

        /// <summary>the average radius of the water</summary>
        [ProtoMember(10), XmlElement("radius")]
        public float Radius = WaterSettings.Default.Radius;

        /// <summary>the maximum height of waves in meters</summary>
        [ProtoMember(15), XmlElement("waveHeight")]
        public float WaveHeight = WaterSettings.Default.WaveHeight;

        public float SubWaveHeight = 0.1f;
        /// <summary>how fast a wave will oscillate</summary>
        [ProtoMember(16), XmlElement("waveSpeed")]
        public float WaveSpeed = WaterSettings.Default.WaveSpeed;
        /// <summary>timer value for syncing waves between clients</summary>
        [ProtoMember(17), XmlElement("waveTimer")]
        public double WaveTimer = 0;
        [ProtoMember(18), XmlElement("waveScale")]
        public float WaveScale = WaterSettings.Default.WaveScale;

        /// <summary>center position of the water</summary>
        [ProtoMember(20), XmlElement("position")]
        public Vector3D Position;

        //Physics properties

        /// <summary>Buoyancy multiplier of the water</summary>
        [ProtoMember(26), XmlElement("buoyancy")]
        public float Buoyancy = WaterSettings.Default.Buoyancy;

        /// <summary>Whether or not the water can support fish</summary>
        [ProtoMember(30), XmlElement("enableFish")]
        public bool EnableFish = WaterSettings.Default.EnableFish;

        /// <summary>Whether or not the water can support seagulls</summary>
        [ProtoMember(31), XmlElement("enableSeagulls")]
        public bool EnableSeagulls = WaterSettings.Default.EnableSeagulls;

        [ProtoMember(33), XmlElement("enableFoam")]
        public bool EnableFoam = WaterSettings.Default.EnableFoam;

        /// <summary>the serializable texture name</summary>
        [ProtoMember(32), XmlElement("texture")]
        public string Texture = WaterSettings.Default.Texture;

        [ProtoIgnore(), XmlIgnore()]
        public MyStringId TextureID = MyStringId.GetOrCompute(WaterSettings.Default.Texture);

        [ProtoMember(35), XmlElement("crushDamage")]
        public float CrushDamage = WaterSettings.Default.CrushDamage;

        [ProtoMember(40), XmlElement("playerDrag")]
        public bool PlayerDrag = WaterSettings.Default.PlayerDrag;

        [ProtoMember(45), XmlElement("transparent")]
        public bool Transparent = WaterSettings.Default.Transparent;

        [ProtoMember(50), XmlElement("lit")]
        public bool Lit = WaterSettings.Default.Lit;

        [ProtoMember(55), XmlElement("collectionRate")]
        public float CollectionRate = WaterSettings.Default.CollectionRate;

        [ProtoMember(60), XmlElement("fogColor")]
        public Vector3 FogColor = WaterSettings.Default.FogColor;

        [ProtoIgnore(), XmlIgnore()]
        public Vector3D tideDirection;

        /// <summary>All entites currently under the water</summary>
        //[XmlIgnore, ProtoIgnore]
        //public List<MyEntity> underWaterEntities = new List<MyEntity>();

        /// <summary>The planet entity</summary>
        [XmlIgnore, ProtoIgnore]
        public MyPlanet planet;

        [XmlIgnore, ProtoIgnore]
        public WaterFace[] waterFaces;

        [XmlIgnore, ProtoIgnore]
        public FastNoiseLite noise;

        [ProtoMember(65), XmlElement("tideHeight")]
        public float TideHeight = WaterSettings.Default.TideHeight;

        [ProtoMember(66), XmlElement("tideSpeed")]
        public float TideSpeed = WaterSettings.Default.TideSpeed;

        [ProtoMember(67), XmlElement("tideTimer")]
        public double TideTimer = 0;

        [ProtoMember(70), XmlElement("material")]
        public string MaterialId = "Water";

        [ProtoMember(75), XmlElement("currentSpeed")]
        public float CurrentSpeed = WaterSettings.Default.CurrentSpeed;

        [ProtoMember(76), XmlElement("currentScale")]
        public float CurrentScale = WaterSettings.Default.CurrentScale;

        [ProtoIgnore, XmlIgnore]
        public PlanetConfig PlanetConfig;

        [ProtoIgnore, XmlIgnore]
        public MaterialConfig Material;

        /// <summary>Provide a planet entity and it will set everything up for you</summary>
        public Water(MyPlanet planet, WaterSettings settings = null, float radiusMultiplier = 1.032f)
        {
            if (settings != null)
            {
                this.Radius = settings.Radius * planet.MinimumRadius;
                this.WaveHeight = settings.WaveHeight;
                this.WaveSpeed = settings.WaveSpeed;
                this.WaveScale = settings.WaveScale;
                this.Buoyancy = settings.Buoyancy;
                this.EnableFish = settings.EnableFish;
                this.EnableSeagulls = settings.EnableSeagulls;
                this.Texture = settings.Texture;
                this.Transparent = settings.Transparent;
                this.Lit = settings.Lit;
                this.FogColor = settings.FogColor;
                this.CollectionRate = settings.CollectionRate;
                this.TideHeight = settings.TideHeight;
                this.TideSpeed = settings.TideSpeed;
                this.EnableFoam = settings.EnableFoam;
                this.CrushDamage = settings.CrushDamage;
                this.PlayerDrag = settings.PlayerDrag;
                this.MaterialId = settings.Material;
                this.CurrentSpeed = settings.CurrentSpeed;
                this.CurrentScale = settings.CurrentScale;
            }
            else
                Radius = planet.MinimumRadius * radiusMultiplier;

            PlanetID = planet.EntityId;

            Position = planet.PositionComp.GetPosition();

            this.planet = planet;

            waterFaces = new WaterFace[WaterData.Directions.Length];
            for (int i = 0; i < WaterData.Directions.Length; i++)
            {
                waterFaces[i] = new WaterFace(this, WaterData.Directions[i]);

            }

            Init();
        }

        /// <summary>Without any arguments is used for Protobuf</summary>
        public Water()
        {
            Init();
        }

        public void Init()
        {
            if (planet == null)
                planet = (MyPlanet)MyEntities.GetEntityById(PlanetID);

            if (planet != null)
            {
                if (!WaterData.PlanetConfigs.TryGetValue(planet.Generator.Id, out PlanetConfig))
                    PlanetConfig = WaterData.PlanetConfigs[planet.Generator.Id] = new PlanetConfig(planet.Generator.Id);
            }

            if (Material == null)
                UpdateMaterial();

            if (noise == null)
                noise = new FastNoiseLite();

            if (TextureID.String != Texture)
                TextureID = MyStringId.GetOrCompute(Texture ?? "JWater");
        }

        public void Simulate()
        {
            WaveTimer += WaveSpeed;
            TideTimer += (TideSpeed / 1000f);
            tideDirection.X = Math.Cos(TideTimer);
            tideDirection.Z = Math.Sin(TideTimer);
        }

        public void UpdateTexture()
        {
            TextureID = MyStringId.GetOrCompute(Texture);
            if (PlanetConfig == null && planet != null)
                if (!WaterData.PlanetConfigs.TryGetValue(planet.Generator.Id, out PlanetConfig))
                {
                    PlanetConfig = WaterData.PlanetConfigs[planet.Generator.Id] = new PlanetConfig(planet.Generator.Id);
                    WaterUtils.ShowMessage("Had to add new material " + MaterialId);
                }
        }

        public void UpdateMaterial()
        {
            if (!WaterData.MaterialConfigs.TryGetValue(MaterialId, out Material))
            {
                //Couldn't find this material for some reason, compensate by setting it to water (default)
                MaterialId = "Water";
                Material = WaterData.MaterialConfigs[MaterialId];
            }
        }

        /// <summary>Returns the closest point to water</summary>
        public Vector3D GetClosestSurfacePoint(Vector3D position, double altitudeOffset = 0)
        {
            Vector3D up = Vector3D.Normalize(position - this.Position);
            return ApplyWavesToSurfaceVector(this.Position + (up * (this.Radius + altitudeOffset)), ref up);
        }

        /// <summary>Returns the closest point using a provided normal</summary>
        public Vector3D GetClosestSurfacePointFromNormal(ref Vector3D up, double altitudeOffset = 0)
        {
            return ApplyWavesToSurfaceVector(this.Position + ((up * (Radius + altitudeOffset))), ref up);
        }

        /// <summary>Returns the closest point to water</summary>
        public Vector3D GetClosestSurfacePointSimple(Vector3D position, float altitudeOffset = 0)
        {
            return this.Position + ((Vector3D.Normalize(position - this.Position) * (Radius + altitudeOffset)));
        }

        public Vector3D ApplyWavesToSurfaceVector(Vector3D position, ref Vector3D up)
        {
            double height = 0;

            if (WaveHeight > 0)
                height += noise.GetNoise((position + this.WaveTimer) * this.WaveScale) * this.WaveHeight;

            if (TideHeight > 0)
                height += Vector3D.Dot(up, tideDirection) * this.TideHeight;

            return position + (height * up);
        }

        public Vector3 GetWaveVelocity(Vector3D up)
        {
            Vector3D pos = this.Position + (up * Radius);
            float h1 = (float)noise.GetNoise((pos + (this.WaveTimer)) * this.WaveScale) * this.WaveHeight;
            float h2 = (float)noise.GetNoise((pos + (this.WaveTimer + this.WaveSpeed)) * this.WaveScale) * this.WaveHeight;

            return (Vector3)up * ((h2 - h1) / (this.WaveSpeed / this.WaveScale));
        }

        public Vector3 GetCurrentVelocity(Vector3D up)
        {
            if (CurrentSpeed == 0)
                return Vector3.Zero;

            float n = (float)noise.GetNoise((up * Radius) * this.CurrentScale);

            return WaterUtils.GetPerpendicularVector(up, n * WaterData.TWOPI) * this.CurrentSpeed;
        }

        public Vector3 GetFluidVelocity(Vector3D up)
        {
            return GetCurrentVelocity(up) + GetWaveVelocity(up);
        }

        public bool IsUnderwater(ref Vector3D position, float altitudeOffset = 0)
        {
            return GetDepthSquared(ref position) + (altitudeOffset * altitudeOffset) < 0;
        }

        /// <summary>Overwater = 0, ExitsWater = 1, EntersWater = 2, Underwater = 3</summary>
        public int Intersects(Vector3D from, Vector3D to)
        {
            if (IsUnderwater(ref from))
            {
                if (IsUnderwater(ref to))
                    return 3; //Underwater
                else
                    return 1; //ExitsWater
            }
            else
            {
                if (IsUnderwater(ref to))
                    return 2; //EntersWater
                else
                    return 0; //Overwater
            }
        }

        /// <summary>Overwater = 0, ExitsWater = 1, EntersWater = 2, Underwater = 3</summary>
        public int Intersects(ref LineD line)
        {
            if (IsUnderwater(ref line.From))
            {
                if (IsUnderwater(ref line.To))
                    return 3; //Underwater
                else
                    return 1; //ExitsWater
            }
            else
            {
                if (IsUnderwater(ref line.To))
                    return 2; //EntersWater
                else
                    return 0; //Overwater
            }
        }

        public int Intersects(ref BoundingSphereD Sphere)
        {
            Vector3D Up = GetUpDirection(ref Sphere.Center) * Sphere.Radius;

            Vector3D CenterUp = Sphere.Center + Up;
            Vector3D CenterDown = Sphere.Center - Up;

            if (IsUnderwater(ref CenterUp))
            {
                if (IsUnderwater(ref CenterDown))
                    return 3; //Underwater
                else
                    return 1;//ExitsWater
            }
            else
            {
                if (IsUnderwater(ref CenterDown))
                    return 2; //EntersWater
                else
                    return 0; //Overwater
            }
        }

        /// <summary>Returns the depth of water a position is at, negative numbers are underwater</summary>
        public double GetDepth(ref Vector3D position)
        {
            return ((this.Position - position).Length() - (this.Position - GetClosestSurfacePoint(position)).Length());
            //return GetClosestSurfacePoint(position).Length() - position.Length();
        }

        /// <summary>Returns the squared depth of water a position is at, negative numbers are underwater</summary>
        public double GetDepthSquared(ref Vector3D position)
        {
            return ((this.Position - position).LengthSquared() - (this.Position - GetClosestSurfacePoint(position)).LengthSquared());
            //return GetClosestSurfacePoint(position).Length() - position.Length();
        }

        /// <summary>Returns the up direction at a position</summary>
        public Vector3D GetUpDirection(ref Vector3D position)
        {
            return Vector3D.Normalize(position - this.Position);
        }

        public override string ToString()
        {
            string text = WaterLocalization.CurrentLanguage.GetWaterSettings;

            text += "\n Radius: " + (Radius / planet.MinimumRadius);
            text += "\n WaveHeight: " + WaveHeight;
            text += "\n WaveSpeed: " + WaveSpeed;
            text += "\n WaveScale: " + WaveScale;
            text += "\n Buoyancy: " + Buoyancy;
            text += "\n Enablefish: " + EnableFish;
            text += "\n Enableseagulls: " + EnableSeagulls;
            text += "\n Texture: " + Texture;
            text += "\n Playerdrag: " + PlayerDrag;
            text += "\n Transparent:" + Transparent;
            text += "\n Lighting: " + Lit;
            text += "\n CollectorRate: " + CollectionRate;
            text += "\n FogColor: " + FogColor;
            text += "\n TideHeight: " + TideHeight;
            text += "\n TideSpeed: " + TideSpeed;
            text += "\n Material: " + MaterialId;
            text += "\n CurrentSpeed: " + CurrentSpeed;
            text += "\n CurrentScale: " + CurrentScale;

            return text;
        }
    }
}
