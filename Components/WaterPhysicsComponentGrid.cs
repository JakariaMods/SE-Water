﻿using Draygo.Drag.API;
using Jakaria.Configs;
using Jakaria.Utils;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.Utils;
using VRageMath;
using VRageRender;
using Jakaria.SessionComponents;
using Sandbox.Definitions;
using SpaceEngineers.Game.Entities.Blocks;
using Sandbox.Game.EntityComponents;
using Sandbox.Game;

namespace Jakaria.Components
{
    public class WaterPhysicsComponentGrid : WaterPhysicsComponentBase
    {
        /// <summary>
        /// True if the entity will use airtightness calculations for buoyancy
        /// </summary>
        private bool _useAirtightness = true;

        /// <summary>
        /// True if the entity needs to recalculate airtightness
        /// </summary>
        private bool _needsRecalculateAirtightness;

        /// <summary>
        /// True if the grid has a valid GasSystem Component
        /// </summary>
        private bool _canRecalculateAirtightness = false;

        private readonly object _buoyancyLock = new object();
        private float _waterDensityMultiplier;

        private float _dragOptimizer;

        private readonly object _extentLock = new object();
        private Vector3 _maxExtent;
        private Vector3 _minExtent;

        private bool _rebuildVolumesArray;

        //Grid Stuff
        public IMyCubeGrid IGrid;
        public MyCubeGrid Grid;
        private Dictionary<Vector3I, BlockVolumeData> _blockVolumesRaw;
        private BlockVolumeData[] _blockVolumes;
        private List<Vector3I> _airtightBlocks;
        private int _blocksUnderwater;
        private int _gasBlocks;
        private List<FunctionalBlockState> _newBlockStates = new List<FunctionalBlockState>();

        //Client Effects
        private MyBillboard[] _cobIndicators;
        private List<AnimatedBillboard> _wakes;

        private readonly WaterSettingsComponent _settingsComponent;
        private readonly BlockDamageComponent _damageComponent;
        private readonly WaterAPIComponent _apiComponent;

        public WaterPhysicsComponentGrid() : base()
        {
            _settingsComponent = Session.Instance.TryGet<WaterSettingsComponent>();
            _damageComponent = Session.Instance.TryGet<BlockDamageComponent>();
            _apiComponent = Session.Instance.TryGet<WaterAPIComponent>();
        }

        /// <summary>
        /// Initalize the component
        /// </summary>
        public override void OnAddedToContainer()
        {
            IGrid = Entity as IMyCubeGrid;
            Grid = Entity as MyCubeGrid;

            base.OnAddedToContainer();

            if (MyAPIGateway.Session?.SessionSettings != null)
                _useAirtightness = MyAPIGateway.Session.SessionSettings.EnableOxygen && MyAPIGateway.Session.SessionSettings.EnableOxygenPressurization;

            NeedsRecalculateBuoyancy = true;

            _airtightBlocks = new List<Vector3I>();

            IGrid.OnBlockAdded += IGrid_OnBlockAdded;
            IGrid.OnBlockRemoved += IGrid_OnBlockRemoved;

            if (!_canRecalculateAirtightness && Grid.GridSystems != null && IGrid.GasSystem != null)
            {
                _canRecalculateAirtightness = true;
                IGrid.GasSystem.OnProcessingDataComplete += RecalculateAirtightness;
            }

            _blockVolumesRaw = new Dictionary<Vector3I, BlockVolumeData>();
        }

        /// <summary>
        /// Called before the entity is removed from the scene.
        /// </summary>
        public override void OnBeforeRemovedFromContainer()
        {
            base.OnBeforeRemovedFromContainer();

            if (_canRecalculateAirtightness && Grid.GridSystems != null && IGrid.GasSystem != null)
                IGrid.GasSystem.OnProcessingDataComplete -= RecalculateAirtightness;

            Cleanup();

            if (_apiComponent != null && _apiComponent.DragClientAPI.Heartbeat)
            {
                DragClientAPI.DragObject value;
                _apiComponent.DragObjects.TryRemove(IGrid, out value);
            }

            IGrid.OnBlockRemoved -= IGrid_OnBlockRemoved;
            IGrid.OnBlockAdded -= IGrid_OnBlockAdded;

            if (_blockVolumes != null)
                foreach (var block in _blockVolumes)
                {
                    if (block != null && block.CorrectState != null && block.Block.FatBlock != null)
                    {
                        IMyFunctionalBlock functionalBlock = block.Block.FatBlock as IMyFunctionalBlock;

                        if (functionalBlock != null)
                        {
                            functionalBlock.Enabled = block.CorrectState.Value;
                            block.CorrectState = null;
                        }
                    }
                }
        }

        protected override void OnSimulatePhysicsChanged(bool value)
        {
            if (value)
            {
                if (_useAirtightness)
                    _needsRecalculateAirtightness = true;
            }

            _recalculateFrequency = WaterUtils.CalculateUpdateFrequency(Grid);
        }

        /// <summary>
        /// Removes any persistent data
        /// </summary>
        private void Cleanup()
        {
            if (_cobIndicators != null)
            {
                lock (_wakes)
                    foreach (var indicator in _cobIndicators)
                    {
                        if (indicator != null)
                            MyTransparentGeometry.RemovePersistentBillboard(indicator);
                    }

                _cobIndicators = null;
            }

            if (_wakes != null)
            {
                lock (_wakes)
                    foreach (var Wake in _wakes)
                    {
                        if (Wake.InScene)
                            MyTransparentGeometry.RemovePersistentBillboard(Wake.Billboard);
                    }

                _wakes = null;
            }
        }

        /// <summary>
        /// When a block is removed from the Grid
        /// </summary>
        private void IGrid_OnBlockRemoved(IMySlimBlock block)
        {
            //Calculate new buoyancy change
            if (_blockVolumesRaw.ContainsKey(block.Position))
            {
                _blockVolumesRaw.Remove(block.Position);
                _rebuildVolumesArray = true;

                NeedsRecalculateBuoyancy = true;
                _recalculateFrequency = WaterUtils.CalculateUpdateFrequency(Grid);

                if (block.FatBlock != null)
                {
                    if (block.FatBlock is IMyAirVent)
                    {
                        _gasBlocks--;

                        //Temporary solution for grids without gasblocks
                        if (_gasBlocks == 0)
                        {
                            _airtightBlocks?.Clear();
                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Block is added to the Grid
        /// </summary>
        private void IGrid_OnBlockAdded(IMySlimBlock block)
        {
            if (block.CubeGrid == IGrid)
            {
                _rebuildVolumesArray = true;

                if (_airtightBlocks == null || !_airtightBlocks.Contains(block.Position))
                {
                    BlockConfig config = WaterData.BlockConfigs[block.BlockDefinition.Id];
                    BlockVolumeData data = _blockVolumesRaw[block.Position] = new BlockVolumeData(block, config);

                    NeedsRecalculateBuoyancy = true;
                    _recalculateFrequency = WaterUtils.CalculateUpdateFrequency(Grid);

                    Vector3D blockPosition = Grid.GridIntegerToWorld(block.Position);
                    data.CurrentlyUnderwater = ClosestWater?.IsUnderwaterGlobal(ref blockPosition, ref WaveModifier) ?? false;

                    if (block.FatBlock != null)
                    {
                        var functionalBlock = block.FatBlock as IMyFunctionalBlock;
                        if (functionalBlock != null)
                        {
                            if (data.CurrentlyUnderwater)
                            {
                                if (!data.Config.FunctionalUnderWater || (FluidPressure > data.Config.MaxFunctionalPressure))
                                {
                                    if (functionalBlock.Enabled)
                                    {
                                        data.CorrectState = functionalBlock.Enabled;
                                        lock (_newBlockStates)
                                        {
                                            _newBlockStates.Add(new FunctionalBlockState
                                            {
                                                Block = functionalBlock,
                                                Enabled = false
                                            });
                                        }
                                    }
                                }
                                else if (data.CorrectState != null)
                                {
                                    lock (_newBlockStates)
                                    {
                                        _newBlockStates.Add(new FunctionalBlockState
                                        {
                                            Block = functionalBlock,
                                            Enabled = data.CorrectState.Value
                                        });
                                    }
                                    data.CorrectState = null;
                                }
                            }
                            else
                            {
                                if (!data.Config.FunctionalAboveWater)
                                {
                                    if (functionalBlock.Enabled)
                                    {
                                        data.CorrectState = functionalBlock.Enabled;
                                        lock (_newBlockStates)
                                        {
                                            _newBlockStates.Add(new FunctionalBlockState
                                            {
                                                Block = functionalBlock,
                                                Enabled = false
                                            });
                                        }
                                    }
                                }
                                else if (data.CorrectState != null)
                                {
                                    lock (_newBlockStates)
                                    {
                                        _newBlockStates.Add(new FunctionalBlockState
                                        {
                                            Block = functionalBlock,
                                            Enabled = data.CorrectState.Value
                                        });
                                    }
                                    data.CorrectState = null;
                                }
                            }
                        }
                    }
                }

                if (block.FatBlock != null)
                {
                    block.FatBlock.IsWorkingChanged += IGrid_OnBlockIsWorkingChanged;

                    if (block.FatBlock is IMyAirVent)
                    {
                        _gasBlocks++;
                    }
                }
            }
        }

        /// <summary>
        /// Called after a block's power state changes. Used to prevent blocks from working when they shouldn't (FunctionalUnderwater/FunctionalAbovewater)
        /// </summary>
        private void IGrid_OnBlockIsWorkingChanged(IMyCubeBlock block)
        {
            IMyFunctionalBlock functionalBlock = block as IMyFunctionalBlock;
            if (functionalBlock != null)
            {
                BlockVolumeData blockData;
                if (block.IsWorking && _blockVolumesRaw.TryGetValue(block.Position, out blockData))
                {
                    if (blockData.CurrentlyUnderwater)
                    {
                        if (!blockData.Config.FunctionalUnderWater)
                        {
                            lock (_newBlockStates)
                            {
                                _newBlockStates.Add(new FunctionalBlockState
                                {
                                    Block = functionalBlock,
                                    Enabled = false
                                });
                            }
                        }
                    }
                    else
                    {
                        if (!blockData.Config.FunctionalAboveWater)
                        {
                            lock (_newBlockStates)
                            {
                                _newBlockStates.Add(new FunctionalBlockState
                                {
                                    Block = functionalBlock,
                                    Enabled = false
                                });
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Called when the grid's airtightness is recalculated, updates the current information for use in threads
        /// </summary>
        private void RecalculateAirtightness()
        {
            _airtightBlocks.Clear();

            if (IGrid.GasSystem != null && !IGrid.GasSystem.IsProcessingData)
            {
                List<IMyOxygenRoom> Rooms = new List<IMyOxygenRoom>();
                if (IGrid.GasSystem.GetRooms(Rooms))
                {
                    foreach (var room in Rooms)
                    {
                        if (room.IsAirtight && room.BlockCount > 0)
                        {
                            _airtightBlocks.AddRange(room.Blocks);
                        }
                    }
                }
            }

            _needsRecalculateAirtightness = false;
        }

        /// <summary>
        /// Secondary simulation method for updating low priority/frequency stuff like drowning
        /// </summary>
        public override void UpdateAfter60()
        {
            base.UpdateAfter60();

            if(Session.Instance.IsClient)
                Grid.ForceDisablePrediction = ClosestWater != null && PercentUnderwater > 0;

            //Recalculate Pressure, Density
            if (ClosestWater != null && Entity.Physics != null)
            {
                Assert.NotNull(Grid, "The Grid is null");
                Assert.NotNull(IGrid, "The IGrid is null");

                Assert.NotNull(ClosestWater.Settings, "The settings is null");
                Assert.NotNull(ClosestWater.Settings.Material, "The material is null");

                _waterDensityMultiplier = ClosestWater.Settings.Material.Density / 1000f;

                if (_useAirtightness)
                {
                    if (!_canRecalculateAirtightness && IGrid.GasSystem != null)
                    {
                        _canRecalculateAirtightness = true;
                        _needsRecalculateAirtightness = true;
                        IGrid.GasSystem.OnProcessingDataComplete += RecalculateAirtightness;
                    }
                }
            }
        }

        /// <summary>
        /// Simulation method for physics
        /// </summary>
        public override void UpdateAfter1()
        {
            base.UpdateAfter1();

            if (Entity.Physics == null || ClosestWater == null)
                return;

            if (!Entity.InScene || Entity.MarkedForClose || (!SimulatePhysics && Vector3.IsZero(_gravity)))
                return;

            if (_needsRecalculateAirtightness)
                RecalculateAirtightness();

            try
            {
                if (FluidDepth < Entity.PositionComp.WorldVolume.Radius)
                {
                    _nextRecalculate--;

                    if (NeedsRecalculateBuoyancy || _nextRecalculate <= 0)
                    {
                        double displacement = 0;

                        _blocksUnderwater = 0;
                        _nextRecalculate = _recalculateFrequency;
                        NeedsRecalculateBuoyancy = false;

                        CenterOfBuoyancy = Vector3D.Zero;

                        _minExtent = Vector3.MaxValue;
                        _maxExtent = Vector3.MinValue;

                        if (_rebuildVolumesArray)
                        {
                            _blockVolumes = _blockVolumesRaw.Values.ToArray();
                            _rebuildVolumesArray = false;
                        }

                        if (_blockVolumes != null)
                        {
                            MyAPIGateway.Parallel.ForEach(_blockVolumes, BlockVolume =>
                            {
                                Vector3D blockWorldPosition = Grid.GridIntegerToWorld(BlockVolume.Block.Position);
                                double blockDepth = ClosestWater.GetDepthGlobal(ref blockWorldPosition, ref WaveModifier);
                                bool blockUnderwater = blockDepth < 0;
                                //MyAPIGateway.Utilities.ShowNotification($"{blockDepth}",16);
                                if (blockUnderwater)
                                {
                                    if (_renderComponent != null)
                                    {
                                        if ((BlockVolume.Block.FatBlock != null && !BlockVolume.Block.FatBlock.IsFunctional))
                                        {
                                            if (!BlockVolume.Config.PlayDamageEffect)
                                            {
                                                BlockVolume.Block.FatBlock.SetDamageEffect(false);
                                            }

                                            if (MyUtils.GetRandomInt(300) == 0)
                                                _effectsComponent.CreateBubble(ref blockWorldPosition, Grid.GridSize);
                                        }

                                        if (ClosestWater.Settings.Material.DrawWakes && _wakes != null && Math.Abs(blockDepth) < Grid.GridSize)
                                        {
                                            Vector3 blockVelocity;
                                            Grid.Physics.GetVelocityAtPointLocal(ref blockWorldPosition, out blockVelocity);
                                            blockVelocity += FluidVelocity;

                                            Vector3 blockVerticalVelocity = Vector3.ProjectOnVector(ref blockVelocity, ref _gravityDirection);
                                            Vector3 blockHorizontalVelocity = blockVelocity - blockVerticalVelocity;
                                            float blockHorizontalSpeed = blockHorizontalVelocity.LengthSquared();

                                            //MyAPIGateway.Utilities.ShowNotification($"Update after 1 {}", 16 * (_recalculateFrequency + 1));

                                            if (blockHorizontalSpeed > 100 && MyUtils.GetRandomInt(8 * _recalculateFrequency) == 0)
                                            {
                                                float radius = Grid.GridSize * (_recalculateFrequency + 1);
                                                MyQuadD quad = new MyQuadD()
                                                {
                                                    Point0 = ClosestWater.GetClosestSurfacePointGlobal(blockWorldPosition + ((_renderComponent.GravityAxisA - _renderComponent.GravityAxisB) * radius), ref WaveModifier),
                                                    Point1 = ClosestWater.GetClosestSurfacePointGlobal(blockWorldPosition + ((_renderComponent.GravityAxisA + _renderComponent.GravityAxisB) * radius), ref WaveModifier),
                                                    Point2 = ClosestWater.GetClosestSurfacePointGlobal(blockWorldPosition + ((-_renderComponent.GravityAxisA + _renderComponent.GravityAxisB) * radius), ref WaveModifier),
                                                    Point3 = ClosestWater.GetClosestSurfacePointGlobal(blockWorldPosition + ((-_renderComponent.GravityAxisA - _renderComponent.GravityAxisB) * radius), ref WaveModifier),
                                                };

                                                lock (_wakes)
                                                    _wakes.Add(new AnimatedBillboard(WaterData.FoamMaterial, WaterData.FoamUVSize, new Vector2(MyUtils.GetRandomInt(0, 4) / 4f, 0f), quad, blockHorizontalVelocity, 60, true, true, WaterData.WakeColor, _renderComponent.AmbientColorIntensity));
                                            }
                                        }
                                    }

                                    if (_damageComponent != null && FluidPressure >= BlockVolume.Config.MaximumPressure && CanCrushBlock(BlockVolume.Block))
                                    {
                                        _damageComponent.DoDamage(BlockVolume.Block, ClosestWater.Settings.CrushDamage * (_recalculateFrequency + 1));

                                        if (_renderComponent != null && MyUtils.GetRandomInt(0, 200) == 0)
                                            _effectsComponent.CreateBubble(ref blockWorldPosition, Grid.GridSize);
                                    }

                                    if (SimulatePhysics)
                                    {
                                        lock (_extentLock)
                                        {
                                            _minExtent = Vector3.Min(_minExtent, BlockVolume.Block.Position);
                                            _maxExtent = Vector3.Max(_maxExtent, BlockVolume.Block.Position);
                                        }

                                        //MyAPIGateway.Utilities.ShowNotification($"{MathHelper.Clamp(-blockDepth / Grid.GridSize, 0, 1)}", 16);
                                        //MyTransparentGeometry.AddPointBillboard(WaterData.BlankMaterial, Vector4.One, blockWorldPosition, 0.25f, 0, blendType: MyBillboard.BlendTypeEnum.AdditiveTop);

                                        double pointDisplacement = BlockVolume.Volume * MathHelper.Clamp(-blockDepth / Grid.GridSize, 0, 1) * BlockVolume.Block.BuildLevelRatio;

                                        if (BlockVolume.Block.FatBlock != null && !BlockVolume.Block.FatBlock.MarkedForClose)
                                        {
                                            if (BlockVolume.Block.FatBlock is IMyGasTank)
                                            {
                                                pointDisplacement *= (1f - (BlockVolume.Block.FatBlock as IMyGasTank).FilledRatio);
                                            }
                                        }

                                        Interlocked.Increment(ref _blocksUnderwater);
                                        lock (_buoyancyLock)
                                        {
                                            displacement += pointDisplacement;
                                            CenterOfBuoyancy += ((Vector3D)(BlockVolume.Block.Min + BlockVolume.Block.Max) / 2.0) * pointDisplacement;
                                        }
                                    }
                                }
                                else
                                {
                                    //Not in water
                                }

                                BlockVolume.CurrentlyUnderwater = blockUnderwater;

                                if (BlockVolume.PreviousUnderwater != blockUnderwater)
                                {
                                    BlockVolume.PreviousUnderwater = blockUnderwater;

                                    if (SimulatePhysics)
                                    {
                                        if (blockUnderwater) //Enters water Enter Water
                                        {
                                            IMyFunctionalBlock functionalBlock = BlockVolume.Block.FatBlock as IMyFunctionalBlock;
                                            if (functionalBlock != null)
                                            {
                                                if (!BlockVolume.Config.FunctionalUnderWater || (FluidPressure > BlockVolume.Config.MaxFunctionalPressure))
                                                {
                                                    if (functionalBlock.Enabled)
                                                    {
                                                        BlockVolume.CorrectState = functionalBlock.Enabled;
                                                        lock (_newBlockStates)
                                                        {
                                                            _newBlockStates.Add(new FunctionalBlockState
                                                            {
                                                                Block = functionalBlock,
                                                                Enabled = false
                                                            });
                                                        }
                                                    }
                                                }
                                                else if (BlockVolume.CorrectState != null)
                                                {
                                                    lock (_newBlockStates)
                                                    {
                                                        _newBlockStates.Add(new FunctionalBlockState
                                                        {
                                                            Block = functionalBlock,
                                                            Enabled = BlockVolume.CorrectState.Value
                                                        });
                                                    }
                                                    BlockVolume.CorrectState = null;
                                                }
                                            }

                                            if (Grid.BlocksDestructionEnabled && Grid.GridGeneralDamageModifier != 0)
                                            {
                                                Vector3 blockVelocity;
                                                Grid.Physics.GetVelocityAtPointLocal(ref blockWorldPosition, out blockVelocity);
                                                blockVelocity += FluidVelocity;

                                                Vector3 blockVerticalVelocity = Vector3.ProjectOnVector(ref blockVelocity, ref _gravityDirection);

                                                float blockVerticalSpeed = blockVerticalVelocity.Length();

                                                if (_renderComponent != null)
                                                {
                                                    if (ClosestWater.Settings.Material.DrawSplashes && blockVerticalSpeed > 5)
                                                    {
                                                        lock (_effectsComponent.SurfaceSplashes)
                                                            _effectsComponent.SurfaceSplashes.Add(new Splash(blockWorldPosition, IGrid.GridSize * (blockVerticalSpeed / 5f)));
                                                    }
                                                }

                                                if (_damageComponent != null && blockVerticalVelocity.IsValid() && blockVerticalSpeed > WaterData.GridImpactDamageSpeed / _waterDensityMultiplier)
                                                {
                                                    _damageComponent.DoDamage(BlockVolume.Block, 50 * blockVerticalSpeed * _waterDensityMultiplier);
                                                }
                                            }
                                        }
                                        else //Exits Water Exit Water
                                        {
                                            IMyFunctionalBlock functionalBlock = BlockVolume.Block.FatBlock as IMyFunctionalBlock;
                                            if (functionalBlock != null)
                                            {
                                                if (!BlockVolume.Config.FunctionalAboveWater)
                                                {
                                                    if (functionalBlock.Enabled)
                                                    {
                                                        BlockVolume.CorrectState = functionalBlock.Enabled;
                                                        lock (_newBlockStates)
                                                        {
                                                            _newBlockStates.Add(new FunctionalBlockState
                                                            {
                                                                Block = functionalBlock,
                                                                Enabled = false
                                                            });
                                                        }
                                                    }
                                                }
                                                else if (BlockVolume.CorrectState != null)
                                                {
                                                    lock (_newBlockStates)
                                                    {
                                                        _newBlockStates.Add(new FunctionalBlockState
                                                        {
                                                            Block = functionalBlock,
                                                            Enabled = BlockVolume.CorrectState.Value
                                                        });
                                                    }
                                                    BlockVolume.CorrectState = null;
                                                }
                                            }

                                            if (_renderComponent != null && ClosestWater.Settings.Material.DrawSplashes)
                                            {
                                                Vector3 blockVelocity;
                                                Grid.Physics.GetVelocityAtPointLocal(ref blockWorldPosition, out blockVelocity);
                                                blockVelocity += FluidVelocity;

                                                Vector3 blockVerticalVelocity = Vector3.ProjectOnVector(ref blockVelocity, ref _gravityDirection);

                                                if (blockVerticalVelocity.LengthSquared() < 25)
                                                    return;

                                                lock (_effectsComponent.SurfaceSplashes)
                                                    _effectsComponent.SurfaceSplashes.Add(new Splash(blockWorldPosition, IGrid.GridSize * (_speed / 5f)));

                                                lock (_effectsComponent.SimulatedSplashes)
                                                    _effectsComponent.SimulatedSplashes.Add(new SimulatedSplash(blockWorldPosition, blockVelocity + (MyUtils.GetRandomVector3Normalized() * 3f), IGrid.GridSize * 1.5f, ClosestWater));

                                                MatrixD matrix = MatrixD.CreateWorld(blockWorldPosition, -_renderComponent.CameraGravityDirection, _renderComponent.GravityAxisA);

                                                MyParticleEffect effect;
                                                if (MyParticlesManager.TryCreateParticleEffect("WaterSplashSmall", ref matrix, ref blockWorldPosition, 0, out effect))
                                                {
                                                    effect.UserColorIntensityMultiplier = ClosestWater.PlanetConfig.ColorIntensity;
                                                }
                                            }
                                        }
                                    }
                                }
                            });

                            foreach (var blockState in _newBlockStates)
                            {
                                if (!blockState.Block.Closed && !blockState.Block.MarkedForClose)
                                {
                                    blockState.Block.Enabled = blockState.Enabled;
                                }
                            }
                            _newBlockStates.Clear();
                        }

                        if (SimulatePhysics)
                        {
                            if (_useAirtightness && _airtightBlocks != null && _airtightBlocks.Count > 0)
                            {
                                float VolumeMultiplier = (Grid.GridSize * Grid.GridSize * Grid.GridSize) * ((Grid.GridSizeEnum == MyCubeSize.Large) ? WaterData.AirtightnessCoefficientLarge : WaterData.AirtightnessCoefficientSmall);
                                float sqrGridSizeHalf = Grid.GridSizeHalf * Grid.GridSizeHalf;

                                MyAPIGateway.Parallel.ForEach(_airtightBlocks, AirtightBlock =>
                                {
                                    Vector3D PointWorldPosition = Grid.GridIntegerToWorld(AirtightBlock);
                                    double PointDepth = ClosestWater.GetDepthSquaredGlobal(ref PointWorldPosition, ref WaveModifier);

                                    if (PointDepth < sqrGridSizeHalf)
                                    {
                                        double PointDisplacement = VolumeMultiplier * Math.Max(Math.Min(-PointDepth / sqrGridSizeHalf, 1), 0);

                                        lock (_buoyancyLock)
                                        {
                                            displacement += PointDisplacement;
                                            CenterOfBuoyancy += (Vector3D)AirtightBlock * PointDisplacement;
                                        }
                                    }
                                });
                            }

                            if (displacement > 0)
                            {
                                PercentUnderwater = ((float)_blocksUnderwater / _blockVolumes.Length);
                                CenterOfBuoyancyLocal = CenterOfBuoyancy / displacement;
                                CenterOfBuoyancy = Grid.GridIntegerToWorld(CenterOfBuoyancy / displacement);
                                BuoyancyForce = -_gravity * ClosestWater.Settings.Material.Density * (float)displacement * ClosestWater.Settings.Buoyancy;

                                lock (_extentLock)
                                {
                                    _minExtent *= Grid.GridSize;
                                    _maxExtent *= Grid.GridSize;
                                    _maxExtent += Grid.GridSizeHalfVector;
                                    _minExtent -= Grid.GridSizeHalfVector;
                                }

                                _dragOptimizer = ClosestWater.Settings.Material.Density * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS;
                            }
                            else
                            {
                                CenterOfBuoyancy = Vector3D.Zero;
                                CenterOfBuoyancyLocal = Vector3D.Zero;
                                BuoyancyForce = Vector3.Zero;
                                _blocksUnderwater = 0;
                                _dragOptimizer = 0;
                                PercentUnderwater = 0;
                            }

                            if (_renderComponent != null)
                            {
                                UpdateEffects();
                            }
                        }
                    }

                    //Apply forces
                    if (SimulatePhysics && _blocksUnderwater > 0)
                    {
                        //Buoyancy
                        if (!Vector3.IsZero(BuoyancyForce, 1e-4f) && BuoyancyForce.IsValid())
                            IGrid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, BuoyancyForce, Grid.GridIntegerToWorld(CenterOfBuoyancyLocal), null);

                        Vector3 localExtents = _maxExtent - _minExtent;

                        if (localExtents.X == 0)
                            localExtents.X = Grid.GridSize;
                        if (localExtents.Y == 0)
                            localExtents.Y = Grid.GridSize;
                        if (localExtents.Z == 0)
                            localExtents.Z = Grid.GridSize;

                        Vector3 localSize = Vector3.Abs(localExtents);

                        //Drag
                        if (_speed > 0.001f)
                        {
                            Vector3 localVelocity = Vector3.TransformNormal(_velocity, Grid.PositionComp.WorldMatrixInvScaled);

                            DragForce.X = _dragOptimizer * (localVelocity.X * localVelocity.X) * (localSize.Z * localSize.Y) * -Math.Sign(localVelocity.X);
                            DragForce.Y = _dragOptimizer * (localVelocity.Y * localVelocity.Y) * (localSize.X * localSize.Z) * -Math.Sign(localVelocity.Y);
                            DragForce.Z = _dragOptimizer * (localVelocity.Z * localVelocity.Z) * (localSize.X * localSize.Y) * -Math.Sign(localVelocity.Z);

                            /*Vector3D Position = center + ((Vector3D)localExtents / 2.0) * Vector3D.Sign(localVelocity) * DragForce;
                            Position /= DragForce.Sum;

                            IGrid.Physics.AddForce(MyPhysicsForceType.ADD_BODY_FORCE_AND_BODY_TORQUE, DragForce, Position, null, null);*/

                            IGrid.Physics.AddForce(MyPhysicsForceType.ADD_BODY_FORCE_AND_BODY_TORQUE, DragForce, null, null);

                            if (_settingsComponent != null && _settingsComponent.Settings.ShowDebug)
                            {
                                Vector3 center = (_minExtent + _maxExtent) * 0.5f;
                                uint renderId = Grid.Render.GetRenderObjectID();

                                Vector3 positionX = new Vector3(center.X + (localExtents.X / 2) * Math.Sign(localVelocity.X), center.Y, center.Z);
                                Vector3 positionY = new Vector3(center.X, center.Y + (localExtents.Y / 2) * Math.Sign(localVelocity.Y), center.Z);
                                Vector3 positionZ = new Vector3(center.X, center.Y, center.Z + (localExtents.Z / 2) * Math.Sign(localVelocity.Z));

                                MyTransparentGeometry.AddLocalPointBillboard(WaterData.BlankMaterial, Color.Red, positionX, renderId, Grid.GridSize, 0, MyBillboard.BlendTypeEnum.AdditiveTop);
                                MyTransparentGeometry.AddLocalPointBillboard(WaterData.BlankMaterial, Color.Green, positionY, renderId, Grid.GridSize, 0, MyBillboard.BlendTypeEnum.AdditiveTop);
                                MyTransparentGeometry.AddLocalPointBillboard(WaterData.BlankMaterial, Color.Blue, positionZ, renderId, Grid.GridSize, 0, MyBillboard.BlendTypeEnum.AdditiveTop);

                                MyTransparentGeometry.AddLocalLineBillboard(WaterData.BlankMaterial, Color.Red, positionX, renderId, Vector3.Right * Math.Sign(localVelocity.X), localVelocity.X, Grid.GridSizeQuarter, MyBillboard.BlendTypeEnum.AdditiveTop);
                                MyTransparentGeometry.AddLocalLineBillboard(WaterData.BlankMaterial, Color.Green, positionY, renderId, Vector3.Up * Math.Sign(localVelocity.Y), localVelocity.Y, Grid.GridSizeQuarter, MyBillboard.BlendTypeEnum.AdditiveTop);
                                MyTransparentGeometry.AddLocalLineBillboard(WaterData.BlankMaterial, Color.Blue, positionZ, renderId, Vector3.Forward * Math.Sign(localVelocity.Z), localVelocity.Z, Grid.GridSizeQuarter, MyBillboard.BlendTypeEnum.AdditiveTop);

                                MyTransparentGeometry.AddLocalLineBillboard(WaterData.BlankMaterial, Color.White, positionX, renderId, Vector3.Right, (float)DragForce.X, Grid.GridSizeQuarter, MyBillboard.BlendTypeEnum.AdditiveTop);
                                MyTransparentGeometry.AddLocalLineBillboard(WaterData.BlankMaterial, Color.White, positionY, renderId, Vector3.Up, (float)DragForce.Y, Grid.GridSizeQuarter, MyBillboard.BlendTypeEnum.AdditiveTop);
                                MyTransparentGeometry.AddLocalLineBillboard(WaterData.BlankMaterial, Color.White, positionZ, renderId, Vector3.Forward, (float)DragForce.Z, Grid.GridSizeQuarter, MyBillboard.BlendTypeEnum.AdditiveTop);
                            }

                            //TODO USE PHYSICS INSTEAD OF SETTING VELOCITY
                            Vector3 velocityDamper = _gravityDirection * (Vector3.Dot(_velocityDirection, _gravityDirection) * _speed * ClosestWater.Settings.Material.Viscosity * _waterDensityMultiplier);
                            IGrid.Physics.LinearVelocity -= velocityDamper;
                        }

                        //Angular Drag
                        if (_angularSpeed > 0.001f && Grid.Physics.Mass != 0)
                        {
                            //https://web.archive.org/web/20160506233549/http://www.randygaul.net/wp-content/uploads/2014/02/RigidBodies_WaterSurface.pdf
                            Vector3 localAngularVelocity = Vector3.TransformNormal(IGrid.Physics.AngularVelocity, Grid.PositionComp.WorldMatrixInvScaled) * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS;

                            Vector3 angularDragForce = new Vector3
                            {
                                X = ClosestWater.Settings.Material.Density * (localAngularVelocity.X * localAngularVelocity.X) * (localSize.Y * localSize.Z) * -Math.Sign(localAngularVelocity.X),
                                Y = ClosestWater.Settings.Material.Density * (localAngularVelocity.Y * localAngularVelocity.Y) * (localSize.X * localSize.Z) * -Math.Sign(localAngularVelocity.Y),
                                Z = ClosestWater.Settings.Material.Density * (localAngularVelocity.Z * localAngularVelocity.Z) * (localSize.X * localSize.Y) * -Math.Sign(localAngularVelocity.Z),
                            };

                            angularDragForce /= Grid.Physics.Mass;

                            IGrid.Physics.AngularVelocity += Vector3.TransformNormal(angularDragForce, Grid.PositionComp.WorldMatrixRef);
                            IGrid.Physics.AngularVelocity *= 0.9925f;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (!MyAPIGateway.Utilities.IsDedicated)
                    MyAPIGateway.Utilities.ShowNotification("Water Error on Grid. Phys-" + SimulatePhysics, 16 * _recalculateFrequency);

                NeedsRecalculateBuoyancy = true;

                WaterUtils.WriteLog(e.ToString());
            }
        }

        /// <summary>
        /// Returns true if the block is capable of being crushed by water pressure (Exposed to water)
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        public bool CanCrushBlock(IMySlimBlock block)
        {
            bool AirtightAtAll = false;
            bool HasMissingNeighbor = false;

            BlockConfig blockConfig;
            if (WaterData.BlockConfigs.TryGetValue(block.BlockDefinition.Id, out blockConfig))
            {
                if (blockConfig.IsPressurized)
                {
                    AirtightAtAll = true;
                }
            }

            foreach (var direction in Base6Directions.IntDirections)
            {
                Vector3I tempPosition = block.Position + direction;

                if (!AirtightAtAll && _airtightBlocks?.Contains(tempPosition) == true)
                {
                    AirtightAtAll = true;
                    continue;
                }

                if (!HasMissingNeighbor && !IGrid.CubeExists(tempPosition))
                {
                    HasMissingNeighbor = true;
                }

                if (AirtightAtAll && HasMissingNeighbor)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Simulation method for updating effects like decals, indicators
        /// </summary>
        private void UpdateEffects()
        {
            if (MyAPIGateway.Utilities.IsDedicated)
                return;

            if (_wakes == null)
            {
                _wakes = new List<AnimatedBillboard>();
            }

            for (int i = _wakes.Count - 1; i >= 0; i--)
            {
                AnimatedBillboard wake = _wakes[i];
                if (wake == null || wake.MarkedForClose)
                {
                    _wakes.RemoveAtFast(i);
                    continue;
                }
                else
                {
                    wake.Simulate();
                    wake.Velocity *= 0.9f;
                }
            }

            if (_settingsComponent.Settings.ShowCenterOfBuoyancy)
            {
                if (_cobIndicators == null)
                {
                    _cobIndicators = new MyBillboard[3];

                    for (int i = 0; i < 3; i++)
                    {
                        _cobIndicators[i] = new MyBillboard()
                        {
                            Color = WaterData.RedColor,
                            ColorIntensity = 10,
                            CustomViewProjection = -1,
                            BlendType = MyBillboard.BlendTypeEnum.AdditiveTop,
                            Material = WaterData.BlankMaterial,
                            UVOffset = Vector2.Zero,
                            UVSize = Vector2.One,
                        };

                        MyTransparentGeometry.AddBillboard(_cobIndicators[i], true);
                    }
                }
                else
                {
                    Vector3D halfExtentsLocal = Grid.PositionComp.LocalAABB.HalfExtents;

                    MyBillboard billboard;
                    Vector3D sideVector;
                    Vector3D cameraNormal = _renderComponent.CameraPosition - CenterOfBuoyancy;
                    Vector3D direction;
                    Vector3D offset;
                    float radius = 0.01f + (((float)cameraNormal.Normalize() / 10f) * 0.01f);

                    //Forward
                    billboard = _cobIndicators[0];
                    direction = Grid.WorldMatrix.Forward;
                    offset = Vector3D.TransformNormal(halfExtentsLocal * Vector3.Forward, Grid.WorldMatrix);
                    sideVector = MyUtils.GetVector3Scaled(Vector3D.Cross(direction, cameraNormal), radius);
                    billboard.Position0 = CenterOfBuoyancy - offset - sideVector;
                    billboard.Position1 = CenterOfBuoyancy + offset - sideVector;
                    billboard.Position2 = CenterOfBuoyancy + offset + sideVector;
                    billboard.Position3 = CenterOfBuoyancy - offset + sideVector;

                    //Right
                    billboard = _cobIndicators[1];
                    direction = Grid.WorldMatrix.Right;
                    offset = Vector3D.TransformNormal(halfExtentsLocal * Vector3.Right, Grid.WorldMatrix);
                    sideVector = MyUtils.GetVector3Scaled(Vector3D.Cross(direction, cameraNormal), radius);
                    billboard.Position0 = CenterOfBuoyancy - offset - sideVector;
                    billboard.Position1 = CenterOfBuoyancy + offset - sideVector;
                    billboard.Position2 = CenterOfBuoyancy + offset + sideVector;
                    billboard.Position3 = CenterOfBuoyancy - offset + sideVector;

                    //Up
                    billboard = _cobIndicators[2];
                    direction = Grid.WorldMatrix.Up;
                    offset = Vector3D.TransformNormal(halfExtentsLocal * Vector3.Up, Grid.WorldMatrix);
                    sideVector = MyUtils.GetVector3Scaled(Vector3D.Cross(direction, cameraNormal), radius);
                    billboard.Position0 = CenterOfBuoyancy - offset - sideVector;
                    billboard.Position1 = CenterOfBuoyancy + offset - sideVector;
                    billboard.Position2 = CenterOfBuoyancy + offset + sideVector;
                    billboard.Position3 = CenterOfBuoyancy - offset + sideVector;
                }
            }
            else if (_cobIndicators != null)
            {
                //Cleanup billboards
                MyTransparentGeometry.RemovePersistentBillboards(_cobIndicators);

                _cobIndicators = null;
            }
        }

        /// <summary>
        /// Class for use in the BlockVolume's array, keeps things organized. Needs to be class to allow mutability
        /// </summary>
        public class BlockVolumeData
        {
            public BlockConfig Config;
            public bool CurrentlyUnderwater;
            public bool PreviousUnderwater;
            public bool? CorrectState;
            public IMySlimBlock Block;
            public double Volume;

            public BlockVolumeData(IMySlimBlock block, BlockConfig config)
            {
                Config = config;
                Block = block;

                if (config.IsPressurized)
                    Volume = config.Volume * (block.CubeGrid.GridSizeEnum == MyCubeSize.Large ? WaterData.AirtightnessCoefficientLarge : WaterData.AirtightnessCoefficientSmall);
                else
                {
                    Volume = config.Volume * (block.CubeGrid.GridSizeEnum == MyCubeSize.Large ? WaterData.BuoyancyCoefficientLarge : WaterData.BuoyancyCoefficientSmall);

                    if (block.FatBlock == null)
                    {
                        Volume *= WaterData.BuoyancyCoefficientArmor;
                    }
                }
            }
        }

        private struct FunctionalBlockState
        {
            public IMyFunctionalBlock Block;
            public bool Enabled;
        }
    }
}