using Jakaria.Components;
using Jakaria.Configs;
using Jakaria.Utils;
using Sandbox.Engine.Physics;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.Utils;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace Jakaria.SessionComponents
{
    /// <summary>
    /// Session component for managing all fish entities
    /// </summary>
    public class WaterFishComponent : SessionComponentBase
    {
        private const string HAT_MODEL = "\\Models\\Cubes\\large\\Hat.mwm";

        private const double TARGET_EPSILON_SQR = 4;
        private const double MAX_TARGET_DISTANCE_SQR = 1000;
        private const int MAX_SPAWN_LOOKUPS = 8;
        private const double MIN_ALTITUDE = 1;
        private const int CHUNK_VIEW_DISTANCE = 1;
        private const int CHUNK_SIZE = 64;
        private const int FISH_PER_CHUNK = 16;

        private List<FishConfig> _configs = new List<FishConfig>(WaterData.FishConfigs.Count);

        private WaterRenderSessionComponent _renderComponent;
        private WaterSyncComponent _syncComponent;

        private Dictionary<Vector3I, FishChunk> _chunks = new Dictionary<Vector3I, FishChunk>();

        private Vector3I _chunkPosition = Vector3I.MaxValue;

        public override void LoadData()
        {
            _renderComponent = Session.Instance.Get<WaterRenderSessionComponent>();
            _syncComponent = Session.Instance.Get<WaterSyncComponent>();

            _syncComponent.OnWaterUpdated += OnWaterUpdated;
            _renderComponent.OnWaterChanged += OnWaterChanged;
        }

        public override void UnloadData()
        {
            _syncComponent.OnWaterUpdated -= OnWaterUpdated;
            _renderComponent.OnWaterChanged -= OnWaterChanged;
        }

        private void OnWaterUpdated(WaterComponent water)
        {
            if (_renderComponent.ClosestWater == water)
            {
                OnWaterChanged(water);
            }
        }

        private void OnWaterChanged(WaterComponent water)
        {
            if (water == null || !water.Settings.EnableFish)
            {
                foreach (var chunk in _chunks)
                {
                    chunk.Value.Dispose();   
                }

                _chunks.Clear();
            }
            else
            {
                OnChunkPositionChanged(_chunkPosition, _chunkPosition);
            }
        }

        private FishConfig GetRandomFish(double pressure)
        {
            int maxWeight = 0;

            _configs.Clear();
            foreach (var configPair in WaterData.FishConfigs)
            {
                FishConfig config = configPair.Value;
                maxWeight += config.SpawnWeight;

                if (pressure > config.MinimumPressure && pressure < config.MaximumPressure)
                {
                    _configs.Add(config);
                }
            }

            if (_configs.Count > 0)
            {
                int randomWeight = MyUtils.GetRandomInt(maxWeight);

                foreach (var config in _configs)
                {
                    randomWeight -= config.SpawnWeight;

                    if (randomWeight <= 0)
                    {
                        return config;
                    }
                }

                return null;
            }

            return null;
        }

        private FishData SpawnFish(Vector3D position, FishConfig fishConfig, WaterComponent water)
        {
            MyEntity entity = new MyEntity
            {
                Save = false,
                SyncFlag = false,
                NeedsWorldMatrix = false,
            };

            if (fishConfig.ModContext == null)
                entity.Init(null, ModContext.ModPath + @"\" + fishConfig.ModelPath, null, null, null);
            else
                entity.Init(null, fishConfig.ModContext.ModPath + @"\" + fishConfig.ModelPath, null, null, null);

            entity.Flags |= EntityFlags.IsNotGamePrunningStructureObject;
            entity.Render.PersistentFlags = MyPersistentEntityFlags2.None;

            MyEntities.Add(entity, false);

            float gravity;
            Vector3 up = -Vector3.Normalize(MyAPIGateway.Physics.CalculateNaturalGravityAt(position, out gravity));

            Quaternion orientation = Quaternion.CreateFromForwardUp(MyUtils.GetRandomPerpendicularVector(up), up);

            MatrixD matrix = MatrixD.CreateFromTransformScale(orientation, Vector3D.Zero, Vector3D.One);
            entity.PositionComp.SetWorldMatrix(ref matrix);

            entity.InScene = true;
            entity.Render.UpdateRenderObject(true, false);

            var data = new FishData
            {
                Config = fishConfig,
                Entity = entity,
                Position = position,
                MoveDirection = orientation,
                Up = up,
                Water = water,
            };

            if (MyUtils.GetRandomInt(100) == 0)
            {
                data.Child = CreateHatEntity();
            }

            return data;
        }

        private MyEntity CreateHatEntity()
        {
            MyEntity entity = new MyEntity
            {
                Save = false,
                SyncFlag = false,
                NeedsWorldMatrix = false,
            };

            entity.Init(null, ModContext.ModPath + HAT_MODEL, null, null, null);
            entity.Flags |= EntityFlags.IsNotGamePrunningStructureObject;
            MyEntities.Add(entity, false);

            entity.InScene = true;
            entity.Render.UpdateRenderObject(true, false);

            return entity;
        }

        private bool IsChunkInView(Vector3I position)
        {
            return position.X >= _chunkPosition.X - CHUNK_VIEW_DISTANCE &&
                   position.Y >= _chunkPosition.Y - CHUNK_VIEW_DISTANCE &&
                   position.Z >= _chunkPosition.Z - CHUNK_VIEW_DISTANCE &&
                   position.X <= _chunkPosition.X + CHUNK_VIEW_DISTANCE &&
                   position.Y <= _chunkPosition.Y + CHUNK_VIEW_DISTANCE &&
                   position.Z <= _chunkPosition.Z + CHUNK_VIEW_DISTANCE;
        }

        private void OnChunkPositionChanged(Vector3I newPosition, Vector3I oldPosition)
        {
            for (int x = oldPosition.X - CHUNK_VIEW_DISTANCE; x <= oldPosition.X + CHUNK_VIEW_DISTANCE; x++)
            {
                for (int y = oldPosition.Y - CHUNK_VIEW_DISTANCE; y <= oldPosition.Y + CHUNK_VIEW_DISTANCE; y++)
                {
                    for (int z = oldPosition.Z - CHUNK_VIEW_DISTANCE; z <= oldPosition.Z + CHUNK_VIEW_DISTANCE; z++)
                    {
                        Vector3I position = new Vector3I(x, y, z);

                        if (!IsChunkInView(position))
                        {
                            FishChunk chunk;
                            if (_chunks.TryGetValue(position, out chunk))
                            {
                                chunk.Dispose();
                                _chunks.Remove(position);
                            }
                        }
                    }
                }
            }

            if (_renderComponent.ClosestWater != null && _renderComponent.ClosestWater.Settings.EnableFish)
            {
                for (int x = newPosition.X - CHUNK_VIEW_DISTANCE; x <= newPosition.X + CHUNK_VIEW_DISTANCE; x++)
                {
                    for (int y = newPosition.Y - CHUNK_VIEW_DISTANCE; y <= newPosition.Y + CHUNK_VIEW_DISTANCE; y++)
                    {
                        for (int z = newPosition.Z - CHUNK_VIEW_DISTANCE; z <= newPosition.Z + CHUNK_VIEW_DISTANCE; z++)
                        {
                            Vector3I position = new Vector3I(x, y, z);
                            if (!_chunks.ContainsKey(position))
                            {
                                FishChunk chunk = _chunks[position] = new FishChunk();

                                chunk.FishDatas = new List<FishData>(FISH_PER_CHUNK);

                                for (int i = 0; i < FISH_PER_CHUNK; i++)
                                {
                                    Vector3D worldPosition = (position * CHUNK_SIZE) + (WaterUtils.GetRandomVector3Abs() * CHUNK_SIZE);

                                    double pressure = _renderComponent.ClosestWater.GetPressureGlobal(ref worldPosition);

                                    if (pressure > 0)
                                    {
                                        FishConfig fishConfig = GetRandomFish(pressure);

                                        if (fishConfig != null && _renderComponent.ClosestWater.IsUnderwaterGlobal(ref worldPosition) && !_renderComponent.ClosestWater.Planet.IsUnderGround(worldPosition))
                                        {
                                            chunk.FishDatas.Add(SpawnFish(worldPosition, fishConfig, _renderComponent.ClosestWater));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public override void UpdateAfterSimulation()
        {
            Vector3I key = Vector3I.Floor(_renderComponent.CameraPosition / CHUNK_SIZE);
            if (_chunkPosition != key)
            {
                Vector3I oldPosition = _chunkPosition;
                _chunkPosition = key;

                OnChunkPositionChanged(key, oldPosition);
            }

            foreach (var chunk in _chunks)
            {
                foreach (var fish in chunk.Value.FishDatas)
                {
                    Vector3 axisAngles = fish.Config.AnimationStrength * ((float)Math.Sin(MyAPIGateway.Session.ElapsedPlayTime.TotalSeconds * fish.Config.AnimationSpeed));

                    Vector3 directionToTarget = Vector3.Normalize(fish.TargetPosition - fish.Position);

                    fish.MoveDirection = Quaternion.Lerp(fish.MoveDirection, Quaternion.CreateFromForwardUp(directionToTarget, fish.Up), fish.Config.TurnSpeed * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS);

                    fish.Position += fish.MoveDirection.Forward * (fish.Config.MoveSpeed * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS);
                    fish.PathTimer += MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS;

                    if (Vector3D.Dot(_renderComponent.CameraDirection, Vector3D.Normalize(fish.Position - _renderComponent.CameraPosition)) > 0)
                    {
                        MatrixD matrixD = MatrixD.CreateFromTransformScale(fish.MoveDirection * Quaternion.CreateFromYawPitchRoll(axisAngles.X, axisAngles.Y, axisAngles.Z), fish.Position, Vector3D.One);
                        fish.Entity.PositionComp.SetWorldMatrix(ref matrixD, updateChildren:false, skipTeleportCheck:true);

                        if (fish.Child != null)
                        {
                            MatrixD hatMatrix = matrixD;
                            hatMatrix.Translation += Vector3.TransformNormal(fish.Config.ChildOffset, hatMatrix);
                            fish.Child.PositionComp.SetWorldMatrix(ref hatMatrix, updateChildren: false, skipTeleportCheck: true);
                        }
                    }

                    double dist = Vector3D.DistanceSquared(fish.Position, fish.TargetPosition);
                    if (dist <= TARGET_EPSILON_SQR || dist > MAX_TARGET_DISTANCE_SQR || fish.PathTimer > fish.Config.AITargetDistance / fish.Config.MoveSpeed)
                    {
                        fish.TargetPosition = GetRandomTargetPosition(fish);
                        fish.PathTimer = 0;
                    }
                }
            }
        }

        public Vector3D GetRandomSpawnPosition(WaterComponent water, Vector3D origin, float radius, float minDepth)
        {
            Vector3D targetPosition;
            bool underWater;
            bool underGround;
            bool inGrid;

            int index = 0;
            do
            {
                index++;
                targetPosition = origin + (MyUtils.GetRandomVector3Normalized() * MyUtils.GetRandomFloat(0, radius));

                underWater = water.IsUnderwaterGlobal(ref targetPosition, -minDepth);
                underGround = WaterUtils.IsUnderGround(water.Planet, targetPosition, -MIN_ALTITUDE);
                inGrid = false;

                if (underWater && !underGround)
                {
                    IHitInfo hitInfo;
                    inGrid = MyAPIGateway.Physics.CastRay(targetPosition, targetPosition + Vector3.Forward, out hitInfo, 0);
                }
            }
            while (index < MAX_SPAWN_LOOKUPS && (!underWater || underGround || inGrid));
            
            if (underGround)
                targetPosition = water.Planet.GetClosestSurfacePointGlobal(ref targetPosition);// + (Vector3D.Normalize(targetPosition - water.Position) * MIN_ALTITUDE);

            if (!underWater)
                targetPosition = water.GetClosestSurfacePointGlobal(ref targetPosition, -minDepth);

            return targetPosition;
        }

        public Vector3D GetRandomTargetPosition(FishData fish)
        {
            return GetRandomSpawnPosition(fish.Water, fish.Position, fish.Config.AITargetDistance, -(WaterData.WaterVisibility + fish.Water.Settings.WaveHeight + fish.Water.Settings.TideHeight));
        }
    }

    public class FishData
    {
        public FishConfig Config;

        public MyEntity Entity;

        public MyEntity Child;

        public Quaternion MoveDirection;

        public Vector3D Position;

        public Vector3D TargetPosition;

        public Vector3D Up;

        public WaterComponent Water;

        public float PathTimer;
    }

    public class FishChunk : IDisposable
    {
        public List<FishData> FishDatas;

        public void Dispose()
        {
            if (FishDatas != null)
            {
                foreach (var fish in FishDatas)
                {
                    fish.Entity.Close();
                    MyEntities.Remove(fish.Entity);
                }
            }
        }
    }
}
