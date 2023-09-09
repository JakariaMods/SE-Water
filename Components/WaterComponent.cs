using Jakaria.Configs;
using Jakaria.SessionComponents;
using Jakaria.Utils;
using Jakaria.Volumetrics;
using ProtoBuf;
using Sandbox.Game.Entities;
using Sandbox.Game.World.Generator;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.ModAPI;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;

namespace Jakaria.Components
{
    /// <summary>
    /// Component that handles water physics on a planet
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

        public IVolumetricSimulation Volumetrics;

        public const float DEPTH_EPSILON = 0.001f;

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
            
            Radius = Planet.MinimumRadius * Settings.Radius;
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

            InScene = true;
        }

        public override void OnBeforeRemovedFromContainer()
        {
            base.OnBeforeRemovedFromContainer();

            Volumetrics?.Dispose();
            Volumetrics = null;
        }

        public void Simulate()
        {
            Radius = Planet.MinimumRadius * Settings.Radius;

            WaveTimer += Settings.WaveSpeed;
            TideTimer += Settings.TideSpeed / 1000f;

            TideDirection.X = Math.Cos(TideTimer);
            TideDirection.Z = Math.Sin(TideTimer);

            if (Settings.Volumetric)
            {
                if (Volumetrics == null)
                {
                    Volumetrics = new FloodFillSimulation(this);
                }

                Volumetrics.Simulate();
            }
            else
            {
                if (Volumetrics != null)
                {
                    Volumetrics.Dispose();
                    Volumetrics = null;
                }
            }
        }

        /// <summary>
        /// Calculates the fluid depth at a position, negative values are below the surface
        /// </summary>
        public double GetDepthGlobal(ref Vector3D worldPosition)
        {
            Vector3D localPosition = Vector3D.Transform(worldPosition, ref WorldMatrixInv);
            return GetDepthLocal(ref localPosition);
        }

        /// <summary>
        /// Calculates the fluid depth at a position in local space, negative values are below the surface
        /// </summary>
        public double GetDepthLocal(ref Vector3D localPosition)
        {
            double fluidDepth;
            Vector3D up = Vector3D.Normalize(localPosition);
            Vector3D surface = GetClosestSurfacePointFromNormalLocal(ref up, out fluidDepth);

            double depth = localPosition.Length() - surface.Length();

            if(fluidDepth > DEPTH_EPSILON)
            {
                if (depth < -fluidDepth)
                    return double.PositiveInfinity;
                else
                    return depth;
            }
            else
            {
                return double.PositiveInfinity;
            }
        }

        /// <summary>
        /// Calculates the fluid depth at a position without square roots, negative values are below the surface
        /// </summary>
        public double GetDepthSquaredGlobal(ref Vector3D worldPosition)
        {
            Vector3D localPosition = Vector3D.Transform(worldPosition, ref WorldMatrixInv);
            return GetDepthSquaredLocal(ref localPosition);
        }

        /// <summary>
        /// Calculates the fluid depth at a position in local space without square roots, negative values are below the surface
        /// </summary>
        public double GetDepthSquaredLocal(ref Vector3D localPosition)
        {
            double fluidDepth;
            Vector3D up = Vector3D.Normalize(localPosition);
            Vector3D surface = GetClosestSurfacePointFromNormalLocal(ref up, out fluidDepth);

            double depth = localPosition.LengthSquared() - surface.LengthSquared();

            if (fluidDepth > DEPTH_EPSILON)
            {
                
                /*if (depth < -fluidDepth)
                    return double.PositiveInfinity;
                else*/
                    return depth;
            }
            else
            {
                return double.PositiveInfinity;
            }
        }

        /// <summary>
        /// Checks if a position is below the surface
        /// </summary>
        public bool IsUnderwaterGlobal(ref Vector3D worldPosition, double altitudeOffset = 0)
        {
            if (altitudeOffset > 0)
                return GetDepthSquaredGlobal(ref worldPosition) < -(altitudeOffset * altitudeOffset);

            return GetDepthSquaredGlobal(ref worldPosition) < (altitudeOffset * altitudeOffset);
        }

        /// <summary>
        /// Checks if a position in local space is below the surface
        /// </summary>
        /// <returns></returns>
        public bool IsUnderwaterLocal(ref Vector3D localPosition, double altitudeOffset = 0)
        {
            if(altitudeOffset > 0)
                return GetDepthSquaredLocal(ref localPosition) < (altitudeOffset * altitudeOffset);

            return GetDepthSquaredLocal(ref localPosition) < -(altitudeOffset * altitudeOffset);
        }

        /// <summary>
        /// Gets the surface position closest to the given position
        /// </summary>
        public Vector3D GetClosestSurfacePointGlobal(ref Vector3D worldPosition, double altitudeOffset = 0)
        {
            Vector3D localPosition = Vector3D.Transform(worldPosition, ref WorldMatrixInv);

            return Vector3D.Transform(GetClosestSurfacePointLocal(ref localPosition, altitudeOffset), ref WorldMatrix);
        }

        /// <summary>
        /// Gets the surface position closest to the given position
        /// </summary>
        public Vector3D GetClosestSurfacePointGlobal(Vector3D worldPosition, double altitudeOffset = 0)
        {
            Vector3D localPosition = Vector3D.Transform(worldPosition, ref WorldMatrixInv);

            return Vector3D.Transform(GetClosestSurfacePointLocal(ref localPosition, altitudeOffset), ref WorldMatrix);
        }

        /// <summary>
        /// Gets the surface position closest to the given position in local space
        /// </summary>
        public Vector3D GetClosestSurfacePointLocal(ref Vector3D localPosition, double altitudeOffset = 0)
        {
            Vector3D localNormal = Vector3D.Normalize(localPosition);

            double _;
            return GetClosestSurfacePointFromNormalLocal(ref localNormal, out _, altitudeOffset);
        }

        /// <summary>
        /// Gets the surface position with a normal in local space
        /// </summary>
        /// <param name="localNormal"></param>
        /// <param name="altitudeOffset"></param>
        /// <returns></returns>
        public Vector3D GetClosestSurfacePointFromNormalLocal(ref Vector3D localNormal, out double fluidDepth, double altitudeOffset = 0)
        {
            double waveHeight;

            if(Volumetrics == null)
            {
                waveHeight = Radius;
                fluidDepth = Radius;
            }
            else
            {
                Volumetrics.GetFluid(localNormal, out fluidDepth, out waveHeight);

                if (fluidDepth <= DEPTH_EPSILON)
                    fluidDepth = double.PositiveInfinity;
                
                waveHeight += fluidDepth;
            }

            if (Settings.WaveHeight != 0)
                waveHeight += FastNoiseLite.GetNoise(((localNormal * Radius) + WaveTimer) * Settings.WaveScale) * Settings.WaveHeight;

            if (Settings.TideHeight != 0)
                waveHeight += Vector3D.Dot(localNormal, TideDirection) * Settings.TideHeight;

            if(altitudeOffset != 0)
                waveHeight += altitudeOffset;

            return waveHeight * localNormal;
        }

        public Vector3 GetFluidVelocityGlobal(Vector3D worldNormal)
        {
            Vector3D localNormal = Vector3D.TransformNormal(worldNormal, ref WorldMatrixInv);
            return Vector3.TransformNormal(GetCurrentVelocityLocal(localNormal) + GetWaveVelocityLocal(localNormal), WorldMatrix);
        }

        public Vector3 GetWaveVelocityLocal(Vector3D localNormal)
        {
            if (Settings.WaveSpeed == 0 || Settings.WaveHeight == 0)
                return Vector3.Zero;

            Vector3D localPosition = (localNormal * Radius);
            float h1 = (float)FastNoiseLite.GetNoise((localPosition + WaveTimer) * Settings.WaveScale) * Settings.WaveHeight;
            float h2 = (float)FastNoiseLite.GetNoise((localPosition + (WaveTimer - Settings.WaveSpeed)) * Settings.WaveScale) * Settings.WaveHeight;
            //TODO, include volumetrics velocity by caching last frame's fluid height and current. Compare the difference to determine if it is flowing up or down
            return (Vector3)localNormal * ((h2 - h1) / (Settings.WaveSpeed / Settings.WaveScale));
        }

        public Vector3D GetCurrentDirectionLocal(Vector3D localNormal)
        {
            if (Settings.CurrentSpeed == 0)
                return Vector3D.Zero;

            double angle = FastNoiseLite.GetNoise((localNormal * Radius) * Settings.CurrentScale) * MathHelperD.TwoPi;

            return WaterUtils.GetPerpendicularVector(localNormal, angle);
        }

        public Vector3 GetCurrentVelocityLocal(Vector3D localNormal)
        {
            if (Settings.CurrentSpeed == 0)
                return Vector3.Zero;

            return (Vector3)GetCurrentDirectionLocal(localNormal) * Settings.CurrentSpeed;
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
            Vector3D localFrom = Vector3D.Transform(line.From, ref WorldMatrixInv);
            Vector3D localTo = Vector3D.Transform(line.To, ref WorldMatrixInv);

            if (IsUnderwaterLocal(ref localFrom))
            {
                if (IsUnderwaterLocal(ref localTo))
                    return 3; //Underwater
                else
                    return 1; //ExitsWater
            }
            else
            {
                if (IsUnderwaterLocal(ref localTo))
                    return 2; //EntersWater
                else
                    return 0; //Overwater
            }
        }

        public Vector3D GetUpDirectionGlobal(ref Vector3D worldPosition)
        {
            Vector3D localPosition = Vector3D.Transform(worldPosition, ref WorldMatrixInv);
            return GetUpDirectionLocal(ref localPosition);
        }

        public Vector3D GetUpDirectionLocal(ref Vector3D localPosition)
        {
            return Vector3D.TransformNormal(Vector3D.Normalize(localPosition), ref WorldMatrix);
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
