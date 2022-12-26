﻿    using Jakaria.Configs;
using Jakaria.Utils;
using ProtoBuf;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.Utils;
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

        /*public SphereTree<WaterNode> Tree = new SphereTree<WaterNode>();
        private SphereTree<WaterNode> _bufferTree = new SphereTree<WaterNode>();

        public const float HEIGHT_EPSILON = 0.01f;
        public const float FLOW_SPEED = 0.01f;
        public const float HEIGHT_OFFSET = 10f;*/

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

            /*for (int i = 0; i < 20000; i++)
            {
                Vector3 normal = MyUtils.GetRandomVector3Normalized();
                Vector3 surface = normal * Planet.AverageRadius;
                float altitude = (float)Planet.GetClosestSurfacePointLocal(ref surface).Length();

                float fluidHeight = Math.Max(9750 - altitude, 0);
                if(fluidHeight > 0)
                {
                    Tree.SetValueLocal(normal, new WaterNode
                    {
                        FluidHeight = fluidHeight,
                        SurfaceDistanceFromCenter = altitude
                    }, Math.Min(FindBestDepth(fluidHeight) + 2, QuadTree<WaterNode>.MAX_DEPTH));
                }
            }*/
        }

        /*private int FindBestDepth(float radius, int depth = 0)
        {
            if (radius > (Radius / Math.Pow(2, QuadTree<WaterNode>.MAX_DEPTH)))
                return FindBestDepth(radius / 2, depth + 1);

            return QuadTree<WaterNode>.MAX_DEPTH - depth;
        }*/

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

            /*var camNormal = Vector3D.Normalize(MyAPIGateway.Session.Camera.Position - WorldMatrix.Translation);

            List<QuadTree<WaterNode>> neigh = new List<QuadTree<WaterNode>>();
            Tree.GetNeighbors(Tree.GetFaceGlobal(camNormal), neigh);

            MyAPIGateway.Utilities.ShowNotification($"{Vector3D.Distance(MyAPIGateway.Session.Camera.Position, Planet.GetClosestSurfacePointGlobal(MyAPIGateway.Session.Camera.Position))}, {FindBestDepth((float)Vector3D.Distance(MyAPIGateway.Session.Camera.Position, Planet.GetClosestSurfacePointGlobal(MyAPIGateway.Session.Camera.Position)))}", 16);

            Stopwatch watch = Stopwatch.StartNew();

            List<QuadTree<WaterNode>> neighbors = new List<QuadTree<WaterNode>>(4);

            Tree.CopyTo(_bufferTree);

            foreach (var face in Tree.GetFaces())
            {
                SimulateDeepestNode(face, neighbors);
            }

            _bufferTree.CopyTo(Tree);

            watch.Stop();
            MyAPIGateway.Utilities.ShowNotification($"Simulation Time: {watch.Elapsed.TotalMilliseconds}ms", 16);*/
        }

        /*private void SimulateDeepestNode(QuadTree<WaterNode> tree, List<QuadTree<WaterNode>> neighbors)
        {
            if (tree.Children == null)
            {
                if (tree.Value.SurfaceDistanceFromCenter == 0)
                {
                    QuadTree<WaterNode> bufferNeighbor = _bufferTree.GetFace(tree.Normal).GetNode(tree.Id);

                    Vector3 normal = Vector3.Normalize(tree.Position);
                    Vector3 surface = normal * Planet.AverageRadius;


                    bufferNeighbor.Value = new WaterNode
                    {
                        FluidHeight = 0,
                        SurfaceDistanceFromCenter = (float)Planet.GetClosestSurfacePointLocal(ref surface).Length()
                    };
                    bufferNeighbor.HasValue = true;

                    tree.Value = bufferNeighbor.Value;
                    tree.HasValue = bufferNeighbor.HasValue;
                }

                if (tree.HasValue)
                {
                    QuadTree<WaterNode> bufferFace = _bufferTree.GetFace(tree.Normal).GetNode(tree.Id);

                    List<QuadTree<WaterNode>> tempNeighbors = new List<QuadTree<WaterNode>>();
                    Tree.GetNeighbors(tree, tempNeighbors);
                    neighbors.EnsureCapacity(tempNeighbors.Count);
                    
                    foreach (var neighbor in tempNeighbors)
                    {
                        if (((tree.Value.FluidHeight + tree.Value.SurfaceDistanceFromCenter) - (neighbor.Value.FluidHeight + neighbor.Value.SurfaceDistanceFromCenter - HEIGHT_OFFSET)) > HEIGHT_EPSILON)
                        {
                            neighbors.Add(neighbor);
                        }
                    }

                    if (neighbors.Count > 0)
                    {
                        float maxFlowPerCell = tree.Value.FluidHeight / neighbors.Count;
                        float actualRemoved = 0;

                        foreach (var neighbor in neighbors)
                        {
                            QuadTree<WaterNode> treeNeighbor = neighbor;
                            QuadTree<WaterNode> bufferNeighbor = _bufferTree.GetFace(neighbor.Normal).GetNode(neighbor.Id);

                            float removedFromCell = Math.Min(((tree.Value.FluidHeight + tree.Value.SurfaceDistanceFromCenter) - (treeNeighbor.Value.FluidHeight + treeNeighbor.Value.SurfaceDistanceFromCenter)) * FLOW_SPEED, maxFlowPerCell);
                            //removedFromCell *= Math.Min(tree.GetDiameter(), treeNeighbor.GetDiameter());

                            bufferNeighbor.Value.FluidHeight += removedFromCell;
                            bufferNeighbor.HasValue = true;
                            actualRemoved += removedFromCell;
                        }
                        
                        bufferFace.Value.FluidHeight -= actualRemoved;
                        neighbors.Clear();
                    }
                }
                
                if (tree.Value.FluidHeight > 1)
                {
                    Vector3D center = (Vector3D)tree.Position;

                    if (Vector3D.Dot(Vector3D.Normalize(center), Vector3D.Normalize(MyAPIGateway.Session.Camera.Position - WorldMatrix.Translation)) > 0)
                    {
                        double size = tree.GetDiameter();
                        Vector3D right = (Vector3D)Base6Directions.GetVector(Base6DirectionsUtils.GetRight(tree.Normal)) * size;
                        Vector3D up = (Vector3D)Base6Directions.GetVector(Base6DirectionsUtils.GetUp(tree.Normal)) * size;

                        MyQuadD quad = new MyQuadD
                        {
                            Point0 = center - right - up,
                            Point1 = center - right + up,
                            Point2 = center + right + up,
                            Point3 = center + right - up,
                        };

                        double radius = tree.Value.SurfaceDistanceFromCenter + tree.Value.FluidHeight;

                        quad.Point0 = Vector3D.Transform(Vector3D.Normalize(quad.Point0) * radius, ref WorldMatrix);
                        quad.Point1 = Vector3D.Transform(Vector3D.Normalize(quad.Point1) * radius, ref WorldMatrix);
                        quad.Point2 = Vector3D.Transform(Vector3D.Normalize(quad.Point2) * radius, ref WorldMatrix);
                        quad.Point3 = Vector3D.Transform(Vector3D.Normalize(quad.Point3) * radius, ref WorldMatrix);

                        if (Vector3D.Dot(Vector3D.Normalize(center), Vector3D.Normalize(MyAPIGateway.Session.Camera.Position - quad.Point0)) < 0)
                            return;

                        Vector4 color = WaterData.BlueColor;

                        MyTransparentGeometry.AddQuad(WaterData.DebugMaterial, ref quad, color, ref Vector3D.Zero);

                        MyQuadD side = quad;
                        side.Point1 = WorldMatrix.Translation;
                        side.Point2 = WorldMatrix.Translation;
                        MyTransparentGeometry.AddQuad(WaterData.DebugMaterial, ref side, color, ref Vector3D.Zero);

                        side = quad;
                        side.Point2 = WorldMatrix.Translation;
                        side.Point3 = WorldMatrix.Translation;
                        MyTransparentGeometry.AddQuad(WaterData.DebugMaterial, ref side, color, ref Vector3D.Zero);
                    }
                }
            }
            else
            {
                foreach (var child in tree.Children)
                {
                    SimulateDeepestNode(child, neighbors);
                }
            }
        }*/

        /// <summary>
        /// Calculates the fluid depth at a position, negative values are below the surface
        /// </summary>
        public double GetDepthGlobal(ref Vector3D worldPosition)
        {
            Vector3D localPosition = Vector3D.Transform(worldPosition, WorldMatrixInv);
            return GetDepthLocal(ref localPosition);
        }

        /// <summary>
        /// Calculates the fluid depth at a position in local space, negative values are below the surface
        /// </summary>
        public double GetDepthLocal(ref Vector3D localPosition)
        {
            Vector3D up = Vector3D.Normalize(localPosition);
            Vector3D surface = GetClosestSurfacePointFromNormalLocal(ref up);

            return localPosition.Length() - surface.Length();
        }

        /// <summary>
        /// Calculates the fluid depth at a position without square roots, negative values are below the surface
        /// </summary>
        public double GetDepthSquaredGlobal(ref Vector3D worldPosition)
        {
            Vector3D localPosition = Vector3D.Transform(worldPosition, WorldMatrixInv);
            return GetDepthSquaredLocal(ref localPosition);
        }

        /// <summary>
        /// Calculates the fluid depth at a position in local space without square roots, negative values are below the surface
        /// </summary>
        public double GetDepthSquaredLocal(ref Vector3D localPosition)
        {
            Vector3D up = Vector3D.Normalize(localPosition);
            Vector3D surface = GetClosestSurfacePointFromNormalLocal(ref up);

            return (localPosition.LengthSquared() - surface.LengthSquared());
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
            Vector3D localPosition = Vector3D.Transform(worldPosition, WorldMatrixInv);

            return Vector3D.Transform(GetClosestSurfacePointLocal(ref localPosition, altitudeOffset), ref WorldMatrix);
        }

        /// <summary>
        /// Gets the surface position closest to the given position
        /// </summary>
        public Vector3D GetClosestSurfacePointGlobal(Vector3D worldPosition, double altitudeOffset = 0)
        {
            Vector3D localPosition = Vector3D.Transform(worldPosition, WorldMatrixInv);

            return Vector3D.Transform(GetClosestSurfacePointLocal(ref localPosition, altitudeOffset), ref WorldMatrix);
        }

        /// <summary>
        /// Gets the surface position closest to the given position in local space
        /// </summary>
        public Vector3D GetClosestSurfacePointLocal(ref Vector3D localPosition, double altitudeOffset = 0)
        {
            Vector3D localNormal = Vector3D.Normalize(localPosition);
            return GetClosestSurfacePointFromNormalLocal(ref localNormal, altitudeOffset);
        }

        /// <summary>
        /// Gets the surface position with a normal in local space
        /// </summary>
        /// <param name="localNormal"></param>
        /// <param name="altitudeOffset"></param>
        /// <returns></returns>
        public Vector3D GetClosestSurfacePointFromNormalLocal(ref Vector3D localNormal, double altitudeOffset = 0)
        {
            Vector3D localPosition = localNormal * Radius;
            double waveHeight = 0;

            if (Settings.WaveHeight != 0)
                waveHeight += FastNoiseLite.GetNoise((localPosition + WaveTimer) * Settings.WaveScale) * Settings.WaveHeight;

            if (Settings.TideHeight != 0)
                waveHeight += Vector3D.Dot(localNormal, TideDirection) * Settings.TideHeight;

            if(altitudeOffset != 0)
                waveHeight += altitudeOffset;

            return localPosition + (waveHeight * localNormal);
        }

        public Vector3 GetFluidVelocityGlobal(Vector3D worldNormal)
        {
            Vector3D localNormal = Vector3D.TransformNormal(worldNormal, WorldMatrixInv);
            return Vector3D.TransformNormal(GetCurrentVelocityLocal(localNormal) + GetWaveVelocityLocal(localNormal), WorldMatrix);
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

            return GetCurrentDirectionLocal(localNormal) * Settings.CurrentSpeed;
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
            Vector3D localFrom = Vector3D.Transform(line.From, WorldMatrixInv);
            Vector3D localTo = Vector3D.Transform(line.To, WorldMatrixInv);

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
