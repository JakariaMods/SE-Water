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
using VRage.Utils;
using VRageMath;

namespace Jakaria
{

    [ProtoContract(UseProtoMembersOnly = true)]
    public class Water
    {
        /// <summary>the entity ID of a planet</summary>
        [ProtoMember(5)]
        public long planetID;

        /// <summary>the average radius of the water</summary>
        [ProtoMember(10)]
        public float radius;
        /// <summary>the current radius of the water</summary>
        [ProtoMember(11)]
        public float currentRadius;

        /// <summary>the maximum height of waves in meters</summary>
        [ProtoMember(15)]
        public float waveHeight = 1f;
        /// <summary>how fast a wave will oscillate</summary>
        [ProtoMember(16)]
        public float waveSpeed = 0.1f;
        /// <summary>timer value for syncing waves between clients</summary>
        [ProtoMember(17)]
        public double waveTimer = 0;
        [ProtoMember(18)]
        public float waveScale = 3f;

        /// <summary>center position of the water</summary>
        [ProtoMember(20)]
        public Vector3D position;

        //Physics properties

        /// <summary>Viscosity of the water</summary>
        [ProtoMember(25)]
        public float viscosity = 0.1f;

        /// <summary>Buoyancy multiplier of the water</summary>
        [ProtoMember(26)]
        public float buoyancy = 1f;

        /// <summary>Whether or not the water can support fish</summary>
        [ProtoMember(30)]
        public bool enableFish = true;

        /// <summary>Whether or not the water can support seagulls</summary>
        [ProtoMember(31)]
        public bool enableSeagulls = true;

        [ProtoMember(33)]
        public bool enableFoam = true;

        /// <summary>the serializable texture name</summary>
        [ProtoMember(32)]
        public string texture = "JWater";

        [ProtoIgnore(), XmlIgnore()]
        public MyStringId textureId = MyStringId.GetOrCompute("JWater");

        [ProtoMember(35)]
        public int crushDepth = 500;

        [ProtoMember(40)]
        public bool playerDrag = true;

        [ProtoMember(45)]
        public bool transparent = true;

        [ProtoMember(50)]
        public bool lit = true;

        [ProtoMember(55)]
        public float collectionRate = 1f;

        [ProtoMember(60)]
        public Vector3D fogColor = new Vector3D(0.1, 0.125, 0.2);

        [ProtoIgnore(), XmlIgnore()]
        public Vector3D tideDirection;

        /// <summary>All entites currently under the water</summary>
        [XmlIgnore, ProtoIgnore]
        public List<MyEntity> underWaterEntities = new List<MyEntity>();

        /// <summary>The planet entity</summary>
        [XmlIgnore, ProtoIgnore]
        public MyPlanet planet;

        [XmlIgnore, ProtoIgnore]
        public WaterFace[] waterFaces;

        /// <summary>The water seed</summary>
        public int seed = 42069;

        [XmlIgnore, ProtoIgnore]
        public FastNoiseLite noise;

        [ProtoMember(65)]
        public float tideHeight = 2f;

        [ProtoMember(66)]
        public float tideSpeed = 0.25f;

        [ProtoMember(67)]
        public double tideTimer = 0;

        /// <summary>Provide a planet entity and it will set everything up for you</summary>
        public Water(MyPlanet planet, WaterSettings settings = null, float radiusMultiplier = 1.032f)
        {
            if (settings != null)
            {
                this.radius = settings.Radius * planet.MinimumRadius;
                this.waveHeight = settings.WaveHeight;
                this.waveSpeed = settings.WaveSpeed;
                this.waveScale = settings.WaveScale;
                this.viscosity = settings.Viscosity;
                this.buoyancy = settings.Buoyancy;
                this.enableFish = settings.EnableFish;
                this.enableSeagulls = settings.EnableSeagulls;
                this.texture = settings.Texture;
                this.crushDepth = settings.CrushDepth;
                this.transparent = settings.Transparent;
                this.lit = settings.Lit;
                this.fogColor = settings.FogColor;
                this.collectionRate = settings.CollectionRate;
            }
            else
                radius = planet.MinimumRadius * radiusMultiplier;

            planetID = planet.EntityId;

            position = planet.PositionComp.GetPosition();

            currentRadius = radius;

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
                planet = (MyPlanet)MyEntities.GetEntityById(planetID);

            if (noise == null)
                noise = new FastNoiseLite(seed);

            if (textureId.String != texture)
                textureId = MyStringId.GetOrCompute(texture?? "JWater");
        }

        public void UpdateTexture()
        {
            textureId = MyStringId.GetOrCompute(texture);
        }

        /// <summary>Returns the closest point to water</summary>
        public Vector3D GetClosestSurfacePoint(Vector3D position, float altitudeOffset = 0)
        {
            Vector3D up = Vector3D.Normalize(position - this.position);
            return GetSurfacePositionWithWaves(this.position + ((up * (this.currentRadius + altitudeOffset))), ref up);
        }

        /// <summary>Returns the closest point to water</summary>
        public Vector3D GetClosestSurfacePointSimple(Vector3D position, float altitudeOffset = 0)
        {
            return this.position + ((Vector3D.Normalize(position - this.position) * (this.currentRadius + altitudeOffset)));
        }

        public Vector3D GetSurfacePositionWithWaves(Vector3D position, ref Vector3D up)
        {
            return position + (GetWaveHeight(position) * up) + (GetTideHeight(up) * up);
        }

        public double GetTideHeight(Vector3D direction)
        {
            return tideHeight * Math.Sqrt((direction.X * direction.X) + (direction.Z * direction.Z)) * Vector3D.Dot(direction, tideDirection);
        }

        public double GetWaveHeight(Vector3D position)
        {
            position = (position + (Vector3D.One * this.waveTimer)) * this.waveScale;
            return noise.GetNoise(position.X, position.Y, position.Z) * this.waveHeight;
        }

        public bool IsUnderwater(ref Vector3D position, float altitudeOffset = 0)
        {
            return GetDepth(position) + altitudeOffset < 0;
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
            Vector3D Up = GetUpDirection(Sphere.Center) * Sphere.Radius;

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
        public float GetDepth(Vector3D position)
        {
            return (float)((this.position - position).Length() - (this.position - GetClosestSurfacePoint(position)).Length());
        }

        /// <summary>Returns the depth of water a position is at without a square root function, negative numbers are underwater</summary>
        public float GetDepthSquared(Vector3D position)
        {
            return (float)((this.position - position).LengthSquared() - (this.position - GetClosestSurfacePoint(position)).LengthSquared());
        }

        public float GetDepthSimple(Vector3D position)
        {
            return (float)((this.position - position).Length() - radius);
        }

        /// <summary>Returns the up direction at a position</summary>
        public Vector3D GetUpDirection(Vector3D position)
        {
            return Vector3.Normalize(position - this.position);
        }

        public override string ToString()
        {
            string text = WaterLocalization.CurrentLanguage.GetWaterSettings;

            text += "\n Radius: " + (radius / planet.MinimumRadius);
            text += "\n WaveHeight: " + waveHeight;
            text += "\n WaveSpeed: " + waveSpeed;
            text += "\n WaveScale: " + waveScale;
            text += "\n Viscosity: " + viscosity;
            text += "\n Buoyancy: " + buoyancy;
            text += "\n Enablefish: " + enableFish;
            text += "\n Enableseagulls: " + enableSeagulls;
            text += "\n Texture: " + texture;
            text += "\n Crushdepth: " + crushDepth;
            text += "\n Playerdrag: " + playerDrag;
            text += "\n Transparent:" + transparent;
            text += "\n Lighting: " + lit;
            text += "\n CollectorRate: " + collectionRate;
            text += "\n FogColor: " + fogColor;
            text += "\n Seed: " + seed;
            text += "\n TideHeight: " + tideHeight;
            text += "\n TideSpeed: " + tideSpeed;

            return text;
        }
    }
}
