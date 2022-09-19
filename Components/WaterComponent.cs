    using Jakaria.Configs;
using Jakaria.Utils;
using ProtoBuf;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRageMath;

namespace Jakaria.Components
{
    /// <summary>
    /// Component that handles water physics
    /// </summary>
    public class WaterComponent : MyEntityComponentBase
    {
        public MyPlanet Planet;

        public WaterSettings Settings;

        public MatrixD WorldMatrix;

        public MatrixD WorldMatrixInv;

        public double Radius;

        public PlanetConfig PlanetConfig;

        public double WaveTimer;

        public double TideTimer;

        public Vector3D TideDirection;

        public bool InScene;

        public override string ComponentTypeDebugString => nameof(WaterComponent);

        public WaterComponent(MyPlanet planet, WaterSettings settings = null)
        {
            Planet = planet;

            if (!WaterData.PlanetConfigs.TryGetValue(planet.Generator.Id, out PlanetConfig))
                WaterData.PlanetConfigs[planet.Generator.Id] = PlanetConfig = new PlanetConfig(planet.Generator.Id);

            if (settings == null)
            {
                if (PlanetConfig != null && PlanetConfig.WaterSettings != null)
                {
                    Settings = PlanetConfig.WaterSettings;
                }
                else
                {
                    Settings = WaterSettings.Default;
                }
            }
            else
            {
                Settings = settings;
            }
        }

        public WaterComponentObjectBuilder Serialize()
        {
            return new WaterComponentObjectBuilder()
            {
                EntityId = Planet.EntityId,
                Settings = Settings,
                Timer = WaveTimer,
                TideTimer = TideTimer,
            };
        }

        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();

            WorldMatrix = Entity.WorldMatrix;
            WorldMatrixInv = Entity.WorldMatrixInvScaled;
            Radius = Planet.MinimumRadius * Settings.Radius;

            InScene = true;
        }

        public override void OnBeforeRemovedFromContainer()
        {
            base.OnBeforeRemovedFromContainer();
        }

        public void Simulate()
        {
            Radius = Planet.MinimumRadius * Settings.Radius;

            WaveTimer += Settings.WaveSpeed;
            TideTimer += Settings.TideSpeed / 1000f;

            TideDirection.X = Math.Cos(TideTimer);
            TideDirection.Z = Math.Sin(TideTimer);
        }

        public double GetDepthGlobal(ref Vector3D worldPosition, double altitudeOffset = 0)
        {
            Vector3D localPosition = Vector3D.Transform(worldPosition, WorldMatrixInv);
            return GetDepthLocal(ref localPosition);
        }

        public double GetDepthLocal(ref Vector3D localPosition)
        {
            Vector3D up = Vector3D.Normalize(localPosition);
            Vector3D surface = ApplyWavesToSurfaceVectorLocal(up * this.Radius, ref up);

            return (localPosition.Length() - surface.Length());
        }

        public double GetDepthSquaredGlobal(ref Vector3D worldPosition)
        {
            Vector3D localPosition = Vector3D.Transform(worldPosition, WorldMatrixInv);
            return GetDepthSquaredLocal(ref localPosition);
        }

        public double GetDepthSquaredLocal(ref Vector3D localPosition)
        {
            Vector3D up = Vector3D.Normalize(localPosition);
            Vector3D surface = ApplyWavesToSurfaceVectorLocal(up * this.Radius, ref up);

            return (localPosition.LengthSquared() - surface.LengthSquared());
        }

        public bool IsUnderwaterGlobal(ref Vector3D worldPosition, double altitudeOffset = 0)
        {
            return GetDepthSquaredGlobal(ref worldPosition) < (altitudeOffset * altitudeOffset) * Math.Sign(altitudeOffset);
        }

        public Vector3D GetClosestSurfacePointGlobal(ref Vector3D worldPosition, double altitudeOffset = 0)
        {
            Vector3D localPosition = Vector3D.Transform(worldPosition, WorldMatrixInv);

            return GetClosestSurfacePointLocal(ref localPosition);
        }

        public Vector3D GetClosestSurfacePointGlobal(Vector3D worldPosition, double altitudeOffset = 0)
        {
            Vector3D localPosition = Vector3D.Transform(worldPosition, WorldMatrixInv);

            return GetClosestSurfacePointLocal(ref localPosition);
        }

        public Vector3D GetClosestSurfacePointLocal(ref Vector3D localPosition, double altitudeOffset = 0)
        {
            Vector3D localNormal = Vector3D.Normalize(localPosition);
            return GetClosestSurfacePointFromNormalLocal(ref localNormal, altitudeOffset);
        }

        public Vector3D GetClosestSurfacePointFromNormalLocal(ref Vector3D localNormal, double altitudeOffset = 0)
        {
            Vector3D localPosition = ((localNormal * (Radius + altitudeOffset)));
            localPosition = ApplyWavesToSurfaceVectorLocal(localPosition, ref localNormal);

            return Vector3D.Transform(localPosition, Entity.WorldMatrix);
        }

        public Vector3D ApplyWavesToSurfaceVectorLocal(Vector3D localPosition, ref Vector3D up)
        {
            double height = 0;
            /*double windSpeed = 0;
            double maxWaveHeight = WaterUtils.GetWaveHeight(Planet, this.Position + position, out windSpeed);

            position += WaterUtils.GetPerpendicularVector(up, FastNoiseLite.GetNoise((position + this.WaveTimer) * Material.SurfaceFluctuationAngleSpeed) * MathHelperD.TwoPi) * FastNoiseLite.GetNoise((position + this.WaveTimer) * Material.SurfaceFluctuationSpeed) * Material.MaxSurfaceFluctuation;*/

            if (Settings.WaveHeight > 0)
                height += FastNoiseLite.GetNoise((localPosition + WaveTimer) * Settings.WaveScale) * Settings.WaveHeight;

            if (Settings.TideHeight > 0)
                height += Vector3D.Dot(up, TideDirection) * Settings.TideHeight;

            return localPosition + (height * up);
        }

        public Vector3 GetFluidVelocityGlobal(Vector3D worldNormal)
        {
            Vector3D localNormal = Vector3D.TransformNormal(worldNormal, WorldMatrixInv);
            return GetCurrentVelocityLocal(localNormal) + GetWaveVelocityLocal(localNormal);
        }

        public Vector3 GetWaveVelocityLocal(Vector3D localNormal)
        {
            if (Settings.WaveSpeed == 0 || Settings.WaveHeight == 0)
                return Vector3.Zero;

            Vector3D localPosition = (localNormal * Radius);
            float h1 = (float)FastNoiseLite.GetNoise((localPosition + WaveTimer) * Settings.WaveScale) * Settings.WaveHeight;
            float h2 = (float)FastNoiseLite.GetNoise((localPosition + (WaveTimer - Settings.WaveSpeed)) * Settings.WaveScale) * Settings.WaveHeight;

            return (Vector3)localNormal * ((h2 - h1) / (Settings.WaveSpeed / Settings.WaveScale));
        }

        public Vector3 GetCurrentVelocityLocal(Vector3D localNormal)
        {
            if (Settings.CurrentSpeed == 0)
                return Vector3.Zero;

            double n = FastNoiseLite.GetNoise((localNormal * Radius) * Settings.CurrentScale);

            return WaterUtils.GetPerpendicularVector(localNormal, n * MathHelperD.TwoPi) * Settings.CurrentSpeed;
        }

        public int IntersectsGlobal(Vector3D from, Vector3D to)
        {
            if (IsUnderwaterGlobal(ref from))
            {
                if (IsUnderwaterGlobal(ref to))
                    return 3; //Underwater
                else
                    return 1; //ExitsWater
            }
            else
            {
                if (IsUnderwaterGlobal(ref to))
                    return 2; //EntersWater
                else
                    return 0; //Overwater
            }
        }

        public int IntersectsGlobal(ref BoundingSphereD Sphere)
        {
            Vector3D up = GetUpDirectionGlobal(ref Sphere.Center) * Sphere.Radius;

            Vector3D centerUp = Sphere.Center + up;
            Vector3D centerDown = Sphere.Center - up;

            if (IsUnderwaterGlobal(ref centerUp))
            {
                if (IsUnderwaterGlobal(ref centerDown))
                    return 3; //Underwater
                else
                    return 1;//ExitsWater
            }
            else
            {
                if (IsUnderwaterGlobal(ref centerDown))
                    return 2; //EntersWater
                else
                    return 0; //Overwater
            }
        }

        public int IntersectsGlobal(ref LineD line)
        {
            if (IsUnderwaterGlobal(ref line.From))
            {
                if (IsUnderwaterGlobal(ref line.To))
                    return 3; //Underwater
                else
                    return 1; //ExitsWater
            }
            else
            {
                if (IsUnderwaterGlobal(ref line.To))
                    return 2; //EntersWater
                else
                    return 0; //Overwater
            }
        }

        public Vector3D GetUpDirectionGlobal(ref Vector3D worldPosition)
        {
            Vector3D localPosition = Vector3D.Transform(worldPosition, WorldMatrixInv);
            return GetUpDirectionLocal(ref localPosition);
        }

        public Vector3D GetUpDirectionLocal(ref Vector3D localPosition)
        {
            return Vector3D.TransformNormal(Vector3D.Normalize(localPosition), WorldMatrix);
        }

        public double GetPressureGlobal(ref Vector3D worldPosition)
        {
            return Math.Max((-GetDepthGlobal(ref worldPosition) * Planet.Generator.SurfaceGravity * Settings.Material.Density) / 1000, 0);
        }
    }

    [ProtoContract, Serializable]
    public class WaterComponentObjectBuilder
    {
        public long EntityId;

        public WaterSettings Settings;

        public double Timer;

        public double TideTimer;
    }
}
