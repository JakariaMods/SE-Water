using Jakaria.Components;
using Jakaria.SessionComponents;
using Jakaria.Utils;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Voxels;
using VRageMath;

namespace Jakaria.Volumetrics
{
    /// <summary>
    /// Simulation that uses flood-fill volumetrics to simulate the surface
    /// </summary>
    public class FloodFillSimulation : IVolumetricSimulation
    {
        public WaterComponent Water;
        public WaterTree Tree;

        private List<WaterTreeNode> _minNodeCache = new List<WaterTreeNode>();
        private List<WaterTreeNode> _simulatedNodes = new List<WaterTreeNode>();

        public const float FLOW_SPEED = 0.2f;
        public const int VOXEL_PRECISION = 2;

        private LODPair[] _lod;
        private double _simulationDistance;

        private Vector3D _centerPosition;
        private Vector3D _previousCenterPosition;

        public FloodFillSimulation(WaterComponent water)
        {
            Water = water;
            water.Planet.RootVoxel.RangeChanged += RootVoxel_RangeChanged;

            _lod = new LODPair[WaterData.VolumetricLOD.Length];
            for (int i = 0; i < _lod.Length; i++)
            {
                _lod[i] = WaterData.VolumetricLOD[i];
                _lod[i].Distance /= water.Radius;

                if (_lod[i].LodLevel <= WaterTreeNode.MIN_DEPTH)
                    _lod[i].Distance = double.MaxValue;
                else
                    _lod[i].Distance = Math.Pow(_lod[i].Distance, 2);
            }

            _simulationDistance = Math.Pow(WaterData.VolumetricSimulationDistance / water.Radius, 2);

            Tree = new WaterTree(water);

            foreach (var face in Tree.Faces)
            {
                GetDeepestNodes(face, _minNodeCache);
            }
        }

        public float AdjustFluid(Vector3 localNormal, float targetAmount)
        {
            WaterTreeNode node = Tree.GetNodeLocal(localNormal);
            node.NewFluidHeight = Math.Max(node.NewFluidHeight, node.NewFluidHeight + targetAmount);
            return targetAmount;
        }

        public void Simulate()
        {
            List<WaterTreeNode> neighbors = new List<WaterTreeNode>(4);

            Stopwatch lodWatch = Stopwatch.StartNew();

            //LOD
            _centerPosition = Vector3D.Transform(MyAPIGateway.Session.Player.GetPosition(), ref Water.WorldMatrixInv);
            //_centerPosition = GetClosestSurfacePointLocal(ref _centerPosition);
            _centerPosition = Vector3DExtensions.ProjectOntoUnitCube(Vector3D.Normalize(_centerPosition));

            if (Vector3D.Distance(_centerPosition, _previousCenterPosition) >= 1 / Water.Radius)
            {
                _previousCenterPosition = _centerPosition;
                Base6Directions.Direction farDirection = Tree.GetFaceLocal(-Vector3D.Normalize(_centerPosition)).Direction;

                foreach (var node in _simulatedNodes)
                {
                    node.Simulated = false;
                }

                _simulatedNodes.Clear();
                MyAPIGateway.Parallel.For(0, _minNodeCache.Count, (i) =>
                {
                    var node = _minNodeCache[i];
                    if (node.Direction == farDirection)
                        return;

                    UpdateLOD(node, ref _centerPosition, _simulatedNodes);
                }, 2000);

                foreach (var node in _simulatedNodes)
                {
                    node.Neighbors.Clear();
                    Tree.GetNeighbors(node, node.Neighbors);
                }

                lodWatch.Stop();
                //MyAPIGateway.Utilities.ShowNotification($"LOD Time: {lodWatch.Elapsed.TotalMilliseconds.ToString("0.000")}ms", 32);
            }
            else
            {
                //MyAPIGateway.Utilities.ShowNotification($"LOD Time: 0.000ms", 16);
            }

            //Simulation
            Stopwatch simWatch = Stopwatch.StartNew();

            for (int i = _simulatedNodes.Count - 1; i >= 0; i--)
            {
                SimulateNode(_simulatedNodes[i]);
            }

            //Buffer update
            MyAPIGateway.Parallel.For(0, _simulatedNodes.Count, (i) =>
            {
                WaterTreeNode face = _simulatedNodes[i];
                face.FluidHeight = face.NewFluidHeight;
            }, 5000);
            /*for (int i = _simulatedNodes.Count - 1; i >= 0; i--)
            {
                QuadTree face = _simulatedNodes[i];
                face.FluidHeight = face.NewFluidHeight;
            }*/

            simWatch.Stop();
            //MyAPIGateway.Utilities.ShowNotification($"Simulation Time: {simWatch.Elapsed.TotalMilliseconds.ToString("0.000")}ms", 16);

            Vector3D cameraNormal = Vector3D.Normalize(Vector3D.Transform(MyAPIGateway.Session.Camera.Position, Water.WorldMatrixInv));
            WaterTreeNode cameraNode = Tree.GetNodeLocal(cameraNormal);

            if (MyAPIGateway.Input.IsMiddleMousePressed())
            {
                if (MyAPIGateway.Input.IsAnyShiftKeyPressed())
                {
                    cameraNode.NewFluidHeight += 10000;
                }
                else if (MyAPIGateway.Input.IsAnyCtrlKeyPressed())
                {
                    cameraNode.NewFluidHeight = Math.Max(cameraNode.NewFluidHeight - 10000, 0);
                }
            }

            if (Session.Instance.Get<WaterSettingsComponent>().Settings.ShowDebug)
            {
                foreach (var face in _simulatedNodes)
                {
                    DebugDraw(face);
                }
            }
        }

        private void RootVoxel_RangeChanged(MyVoxelBase storage, Vector3I minVoxelChanged, Vector3I maxVoxelChanged, MyStorageDataTypeFlags changedData)
        {
            if (Water.Settings.Volumetric == false)
                return;

            if (changedData == MyStorageDataTypeFlags.Content || changedData == MyStorageDataTypeFlags.All)
            {
                double radius = (maxVoxelChanged - minVoxelChanged).Length() / 2;

                for (int x = minVoxelChanged.X; x <= maxVoxelChanged.X; x += VOXEL_PRECISION)
                {
                    for (int y = minVoxelChanged.Y; y <= maxVoxelChanged.Y; y += VOXEL_PRECISION)
                    {
                        for (int z = minVoxelChanged.Z; z <= maxVoxelChanged.Z; z += VOXEL_PRECISION)
                        {
                            Vector3I voxelPos = new Vector3I(x, y, z);
                            Vector3D worldPosition;
                            MyVoxelCoordSystems.VoxelCoordToWorldPosition(storage.WorldMatrix, storage.PositionLeftBottomCorner, storage.SizeInMetresHalf, ref voxelPos, out worldPosition);

                            UpdateSurfaceDistance(storage, worldPosition, radius);
                        }
                    }
                }
            }
        }

        private void UpdateSurfaceDistance(MyVoxelBase storage, Vector3D worldPosition, double radius)
        {
            Vector3D worldNormal = Vector3D.Normalize(worldPosition - Water.WorldMatrix.Translation);
            WaterTreeNode node = Tree.GetNodeGlobal(worldNormal);
            Vector3D offset = worldNormal * radius;

            LineD line = new LineD(worldPosition - offset, worldPosition + offset);
            Vector3D? hitPosition;

            if (storage.GetIntersectionWithLine(ref line, out hitPosition, useCollisionModel: true))
            {
                double distance = Vector3D.Distance(Water.WorldMatrix.Translation, hitPosition.Value);

                if (distance < node.SurfaceDistanceFromCenter)
                {
                    node.SurfaceDistanceFromCenter = distance + WaterTreeNode.SURFACE_OFFSET;

                    MarkNodeEdited(node);
                }
            }
        }

        private void MarkNodeEdited(WaterTreeNode node)
        {
            node.Edited = true;

            if (node.Children == null && node.Depth < WaterData.MaxLODDepth)
            {
                node.Split();
                //TODO NEED TO UPDATE NEIGHBORS
                foreach (var child in node.Children)
                {
                    MarkNodeEdited(node);
                }
            }

            if (node.Parent != null)
            {
                MarkNodeEdited(node.Parent);
            }
        }

        private void GetDeepestNodes(WaterTreeNode tree, List<WaterTreeNode> nodes)
        {
            if (tree.Children == null)
            {
                nodes.Add(tree);
            }
            else
            {
                foreach (var child in tree.Children)
                {
                    GetDeepestNodes(child, nodes);
                }
            }
        }

        private int GetLOD(WaterTreeNode tree, ref Vector3D position)
        {
            double distanceToPlayer = Vector3D.DistanceSquared(position, tree.Position);

            for (int i = 0; i < _lod.Length; i++)
            {
                LODPair lodLevel = _lod[i];
                if (distanceToPlayer < lodLevel.Distance)
                {
                    return lodLevel.LodLevel;
                }
            }

            return WaterTreeNode.MIN_DEPTH;
        }

        //TODO VOLUMETRICS
        private void UpdateLOD(WaterTreeNode tree, ref Vector3D centerPosition, List<WaterTreeNode> simulatedNodes)
        {
            if (!tree.Edited)
            {
                int bestDepth = GetLOD(tree, ref centerPosition);

                if (tree.Children == null)
                {
                    if (tree.Depth < bestDepth)
                    {
                        tree.Split();
                    }
                }
                else
                {
                    if (tree.Depth >= bestDepth)
                    {
                        tree.Reduce();
                    }
                }
            }

            if (tree.Children == null)
            {
                if (tree.Edited || tree.Depth > WaterTreeNode.MIN_DEPTH)
                {
                    lock (simulatedNodes)
                        simulatedNodes.Add(tree);
                    tree.Simulated = true;
                }
            }
            else
            {
                foreach (var child in tree.Children)
                {
                    UpdateLOD(child, ref centerPosition, simulatedNodes);
                }
            }
        }

        //TODO VOLUMETRICS
        private static void SimulateNode(WaterTreeNode tree)
        {
            double volumeRemoved = 0;
            double area = tree.Diameter * tree.Diameter;
            double maxVolumePerCell = (area * tree.FluidHeight) / tree.Neighbors.Count;

            double volumeOptimizer = area * FLOW_SPEED;

            for (int i = 0; i < tree.Neighbors.Count; i++)
            {
                WaterTreeNode neighbor = tree.Neighbors[i];
                double heightDifference = (tree.FluidHeight + tree.SurfaceDistanceFromCenter) - (neighbor.FluidHeight + neighbor.SurfaceDistanceFromCenter);
                if (heightDifference > 0)
                {
                    double volume = volumeOptimizer * heightDifference;
                    double removedFromCell = Math.Min(volume, maxVolumePerCell);

                    //TODO slow down distribution for areas with different sizes

                    neighbor.NewFluidHeight += removedFromCell / (neighbor.Diameter * neighbor.Diameter);
                    volumeRemoved += removedFromCell;
                }
            }

            tree.NewFluidHeight -= volumeRemoved / (tree.Diameter * tree.Diameter);
        }

        private void DebugDraw(WaterTreeNode tree)
        {
            Vector3D center = tree.Position;

            Vector3D right = Base6Directions.GetIntVector(Base6DirectionsUtils.GetRight(tree.Direction)) * tree.Diameter;
            Vector3D up = Base6Directions.GetIntVector(Base6DirectionsUtils.GetUp(tree.Direction)) * tree.Diameter;

            MyQuadD quad = new MyQuadD
            {
                Point0 = center - right - up,
                Point1 = center - right + up,
                Point2 = center + right + up,
                Point3 = center + right - up,
            };

            double radius = tree.SurfaceDistanceFromCenter + tree.FluidHeight;

            MyQuadD normals = new MyQuadD
            {
                Point0 = Vector3D.Normalize(quad.Point0),
                Point1 = Vector3D.Normalize(quad.Point1),
                Point2 = Vector3D.Normalize(quad.Point2),
                Point3 = Vector3D.Normalize(quad.Point3),
            };

            Vector3D cameraNormal = Vector3D.Normalize(Vector3D.Transform(MyAPIGateway.Session.Camera.Position, Water.WorldMatrixInv));
            WaterTreeNode cameraNode = Tree.GetNodeLocal(cameraNormal);

            Vector4 surfaceColor;
            if (tree == cameraNode)
                surfaceColor = WaterData.WhiteColor;
            else if (tree.Neighbors.Contains(cameraNode))
                surfaceColor = WaterData.RedColor;
            else if (tree.FluidHeight > WaterComponent.DEPTH_EPSILON)
                surfaceColor = WaterData.BlueColor;
            else
                surfaceColor = WaterData.GreenColor;

            quad.Point0 = normals.Point0 * radius;
            quad.Point1 = normals.Point1 * radius;
            quad.Point2 = normals.Point2 * radius;
            quad.Point3 = normals.Point3 * radius;

            quad.Point0 = Vector3D.Transform(quad.Point0, ref Water.WorldMatrix);
            quad.Point1 = Vector3D.Transform(quad.Point1, ref Water.WorldMatrix);
            quad.Point2 = Vector3D.Transform(quad.Point2, ref Water.WorldMatrix);
            quad.Point3 = Vector3D.Transform(quad.Point3, ref Water.WorldMatrix);

            MyTransparentGeometry.AddQuad(WaterData.DebugMaterial, ref quad, surfaceColor, ref Vector3D.Zero);
        }

        public void Dispose()
        {
            Water.Planet.RootVoxel.RangeChanged -= RootVoxel_RangeChanged;
        }

        public void GetFluid(Vector3D localNormal, out double depth, out double surface)
        {
            WaterTreeNode node = Tree.GetNodeLocal(localNormal);
            depth = node.Depth;
            surface = node.SurfaceDistanceFromCenter;
        }
    }
}
