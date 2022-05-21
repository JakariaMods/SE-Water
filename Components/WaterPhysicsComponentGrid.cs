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
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace Jakaria.Components
{
    public class WaterPhysicsComponentGrid : WaterPhysicsComponentBase
    {
        /// <summary>
        /// Helper bool. True if the entity will use airtightness calculations for buoyancy
        /// </summary>
        public bool UseAirtightness { get; protected set; } = true;

        /// <summary>
        /// Used only on grids. If set true it will recalculate the airtightness of grid next frame.
        /// </summary>
        public bool NeedsRecalculateAirtightness { get; protected set; } = false;

        /// <summary>
        /// True if the grid has a valid GasSystem Component
        /// </summary>
        public bool CanRecalculateAirtightness { get; protected set; } = false;

        private readonly object BuoyancyLock = new object();
        private float WaterDensityMultiplier;

        private float _dragOptimizer;
        private double maxRadius;

        //Grid Stuff
        public IMyCubeGrid IGrid;
        public MyCubeGrid Grid;
        private Dictionary<Vector3I, BlockVolumeData> _blockVolumesRaw;
        private BlockVolumeData[] _lockVolumes;
        private List<Vector3I> _airtightBlocks;
        private int _blocksUnderwater;
        private int _gasBlocks;
        private Vector3D _centerOfMass;
        private MaterialConfig _material;

        //Client Effects
        private MyBillboard[] COBIndicators;
        private List<AnimatedBillboard> Wakes;
        //List<uint> Decals; TODO?

        /// <summary>
        /// Initalize the component
        /// </summary>
        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();

            if (MyAPIGateway.Session?.SessionSettings != null)
                UseAirtightness = MyAPIGateway.Session.SessionSettings.EnableOxygen && MyAPIGateway.Session.SessionSettings.EnableOxygenPressurization;

            NeedsRecalculateBuoyancy = true;

            IGrid = Entity as IMyCubeGrid;
            Grid = Entity as MyCubeGrid;

            _airtightBlocks = new List<Vector3I>();

            IGrid.OnBlockAdded += IGrid_OnBlockAdded;
            IGrid.OnBlockRemoved += IGrid_OnBlockRemoved;

            try
            {
                if (!CanRecalculateAirtightness && IGrid.GasSystem != null)
                {
                    CanRecalculateAirtightness = true;
                    IGrid.GasSystem.OnProcessingDataComplete += RecalculateAirtightness;
                }
            }
            catch
            {

            }

            _blockVolumesRaw = new Dictionary<Vector3I, BlockVolumeData>();
        }

        /// <summary>
        /// Called before the entity is removed from the scene.
        /// </summary>
        public override void OnBeforeRemovedFromContainer()
        {
            base.OnBeforeRemovedFromContainer();

            if (CanRecalculateAirtightness && IGrid.GasSystem != null)
                IGrid.GasSystem.OnProcessingDataComplete -= RecalculateAirtightness;

            Cleanup();

            IGrid.OnBlockRemoved -= IGrid_OnBlockRemoved;
            IGrid.OnBlockAdded -= IGrid_OnBlockAdded;
        }

        protected override void OnSimulatePhysicsChanged(bool value)
        {
            if (value)
            {
                if (UseAirtightness)
                    NeedsRecalculateAirtightness = true;
            }
        }

        /// <summary>
        /// Removes any persistent data
        /// </summary>
        private void Cleanup()
        {
            if (COBIndicators != null)
            {
                lock (Wakes)
                    foreach (var Indicator in COBIndicators)
                    {
                        if (Indicator != null)
                            MyTransparentGeometry.RemovePersistentBillboard(Indicator);
                    }

                COBIndicators = null;
            }

            if (Wakes != null)
            {
                lock (Wakes)
                    foreach (var Wake in Wakes)
                    {
                        if(Wake.InScene)
                            MyTransparentGeometry.RemovePersistentBillboard(Wake.Billboard);
                    }

                Wakes = null;
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
                _lockVolumes = _blockVolumesRaw.Values.ToArray();
                NeedsRecalculateBuoyancy = true;
                recalculateFrequency = WaterUtils.CalculateUpdateFrequency(Grid);
            }

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

        /// <summary>
        /// Block is added to the Grid
        /// </summary>
        private void IGrid_OnBlockAdded(IMySlimBlock block)
        {
            if (_airtightBlocks == null || !_airtightBlocks.Contains(block.Position))
            {
                BlockConfig config = WaterData.BlockConfigs[block.BlockDefinition.Id];
                BlockVolumeData data = _blockVolumesRaw[block.Position] = new BlockVolumeData(block.Position, config, block);
                _lockVolumes = _blockVolumesRaw.Values.ToArray();
                NeedsRecalculateBuoyancy = true;
                recalculateFrequency = WaterUtils.CalculateUpdateFrequency(Grid);

                Vector3D blockPosition = Grid.GridIntegerToWorld(block.Position);
                data.CurrentlyUnderwater = ClosestWater?.IsUnderwater(ref blockPosition) ?? false;

                if (block.FatBlock != null)
                {
                    var functionalBlock = block.FatBlock as IMyFunctionalBlock;
                    if (functionalBlock != null)
                    {
                        if (data.CurrentlyUnderwater)
                        {
                            if (!data.Config.FunctionalUnderWater || (FluidPressure > data.Config.MaxFunctionalPressure))
                            {
                                if (functionalBlock is IMyThrust)
                                {
                                    if (data.Config.FunctionalUnderWater)
                                    {
                                        (functionalBlock as IMyThrust).ThrustMultiplier = 1;
                                    }
                                    else
                                    {
                                        (functionalBlock as IMyThrust).ThrustMultiplier = 0;
                                        (functionalBlock as MyThrust).CurrentStrength = 0;
                                    }
                                }
                                else if (functionalBlock.Enabled)
                                {
                                    data.CorrectState = functionalBlock.Enabled;
                                    functionalBlock.Enabled = false;
                                }
                            }
                            else if (data.CorrectState != null)
                            {
                                functionalBlock.Enabled = data.CorrectState.Value;
                                data.CorrectState = null;
                            }
                        }
                        else
                        {
                            if (!data.Config.FunctionalAboveWater)
                            {
                                if (functionalBlock is IMyThrust)
                                {
                                    if (data.Config.FunctionalAboveWater)
                                    {
                                        (functionalBlock as IMyThrust).ThrustMultiplier = 1;
                                    }
                                    else
                                    {
                                        (functionalBlock as IMyThrust).ThrustMultiplier = 0;
                                        (functionalBlock as MyThrust).CurrentStrength = 0;
                                    }
                                }
                                else if (functionalBlock.Enabled)
                                {
                                    data.CorrectState = functionalBlock.Enabled;
                                    functionalBlock.Enabled = false;
                                }
                            }
                            else if (data.CorrectState != null)
                            {
                                functionalBlock.Enabled = data.CorrectState.Value;
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

        /// <summary>
        /// Called after a block's power state changes. Used to prevent blocks from working when they shouldn't (FunctionalUnderwater/FunctionalAbovewater)
        /// </summary>
        private void IGrid_OnBlockIsWorkingChanged(IMyCubeBlock block)
        {
            BlockVolumeData blockData;
            if (block.IsWorking && (block is IMyFunctionalBlock) && _blockVolumesRaw.TryGetValue(block.Position, out blockData))
            {
                if (blockData.CurrentlyUnderwater)
                {
                    if (!blockData.Config.FunctionalUnderWater)
                    {
                        if (block is IMyThrust)
                        {

                        }
                        else
                        {
                            (block as IMyFunctionalBlock).Enabled = false;
                        }
                    }
                }
                else
                {
                    if (!blockData.Config.FunctionalAboveWater)
                    {
                        if (block is IMyThrust)
                        {

                        }
                        else
                        {
                            (block as IMyFunctionalBlock).Enabled = false;
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
                    foreach (var Room in Rooms)
                    {
                        if (Room.IsAirtight && Room.BlockCount > 0)
                        {
                            _airtightBlocks.AddRange(Room.Blocks);
                        }
                    }
                }
            }

            NeedsRecalculateAirtightness = false;
        }

        /// <summary>
        /// Secondary simulation method for updating low priority/frequency stuff like drowning
        /// </summary>
        public override void UpdateAfter60()
        {
            base.UpdateAfter60();

            if (!MyAPIGateway.Session.IsServer)
                Grid.ForceDisablePrediction = !SimulateEffects && PercentUnderwater > 0;

            _material = ClosestWater?.Material;

            //Recalculate Pressure, Density
            if (ClosestWater != null && Entity.Physics != null)
            {
                WaterDensityMultiplier = _material.Density / 1000f;

                if (UseAirtightness)
                {
                    if (!CanRecalculateAirtightness && IGrid.GasSystem != null)
                    {
                        CanRecalculateAirtightness = true;
                        NeedsRecalculateAirtightness = true;
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

            if (Entity.Physics == null || ClosestWater == null || _material == null)
                return;

            _material = ClosestWater?.Material;

            try
            {
                if (SimulateEffects || MyAPIGateway.Session.IsServer)
                {
                    if (ClosestWater == null || !Entity.InScene || Entity.Physics == null || Entity.MarkedForClose || (!SimulatePhysics && Vector3.IsZero(Entity.Physics.Gravity)) || Entity.Physics.Mass == 0)
                        return;

                    if (NeedsRecalculateAirtightness)
                        RecalculateAirtightness();

                    if (FluidDepth < Entity.PositionComp.WorldVolume.Radius)
                    {
                        nextRecalculate--;

                        if (NeedsRecalculateBuoyancy || nextRecalculate <= 0)
                        {
                            double displacement = 0;

                            _blocksUnderwater = 0;
                            nextRecalculate = recalculateFrequency;
                            NeedsRecalculateBuoyancy = false;

                            maxRadius = 0;

                            CenterOfBuoyancy = Vector3D.Zero;
                            _centerOfMass = Grid.Physics.CenterOfMassWorld;

                            if (_lockVolumes != null)
                            {
                                MyAPIGateway.Parallel.ForEach(_lockVolumes, BlockVolume =>
                                {
                                    if (BlockVolume.Config.Volume > 0)
                                    {
                                        Vector3D pointWorldPosition = Grid.GridIntegerToWorld(BlockVolume.Key);
                                        double pointDepth = ClosestWater.GetDepth(ref pointWorldPosition);
                                        bool pointUnderwater = pointDepth < Grid.GridSizeHalf;

                                        if (pointUnderwater)
                                        {
                                            if (SimulateEffects)
                                            {
                                                if ((BlockVolume.Block.FatBlock != null && !BlockVolume.Block.FatBlock.IsFunctional) && MyUtils.GetRandomInt(300) == 0)
                                                {
                                                    if (!BlockVolume.Config.PlayDamageEffect)
                                                    {
                                                        if (BlockVolume.Block.FatBlock != null)
                                                        {
                                                            BlockVolume.Block.FatBlock.SetDamageEffect(false);
                                                        }
                                                    }

                                                    WaterModComponent.Static.CreateBubble(ref pointWorldPosition, Grid.GridSize);
                                                }

                                                if (_material.DrawWakes && Wakes != null && Math.Abs(pointDepth) < Grid.GridSizeHalf)
                                                {
                                                    Vector3 BlockVelocity = Grid.Physics.GetVelocityAtPoint(pointWorldPosition);
                                                    Vector3 BlockVerticalVelocity = Vector3.ProjectOnVector(ref BlockVelocity, ref gravityDirection);
                                                    Vector3 BlockHorizontalVelocity = BlockVelocity - BlockVerticalVelocity;
                                                    float BlockHorizontalSpeed = BlockHorizontalVelocity.LengthSquared();

                                                    if (BlockHorizontalSpeed > 100 && MyUtils.GetRandomInt(6 * recalculateFrequency) == 0)
                                                    {
                                                        float radius = Math.Max(Grid.GridSize, 1);
                                                        MyQuadD quad = new MyQuadD()
                                                        {
                                                            Point0 = ClosestWater.GetClosestSurfacePointGlobal(pointWorldPosition + ((WaterModComponent.Static.Session.GravityAxisA - WaterModComponent.Static.Session.GravityAxisB) * radius)),
                                                            Point1 = ClosestWater.GetClosestSurfacePointGlobal(pointWorldPosition + ((WaterModComponent.Static.Session.GravityAxisA + WaterModComponent.Static.Session.GravityAxisB) * radius)),
                                                            Point2 = ClosestWater.GetClosestSurfacePointGlobal(pointWorldPosition + ((-WaterModComponent.Static.Session.GravityAxisA + WaterModComponent.Static.Session.GravityAxisB) * radius)),
                                                            Point3 = ClosestWater.GetClosestSurfacePointGlobal(pointWorldPosition + ((-WaterModComponent.Static.Session.GravityAxisA - WaterModComponent.Static.Session.GravityAxisB) * radius)),
                                                        };

                                                        lock(Wakes)
                                                            Wakes.Add(new AnimatedBillboard(WaterData.FoamMaterial, WaterData.FoamUVSize, new Vector2(MyUtils.GetRandomInt(0, 4) / 4f, 0f), quad, BlockHorizontalVelocity, (recalculateFrequency + 1) + (int)(Math.Sqrt(BlockHorizontalSpeed) * 3), true, true, WaterData.WakeColor, WaterModComponent.Static.Session.AmbientColorIntensity));
                                                    }
                                                }
                                            }

                                            if (FluidPressure >= BlockVolume.Config.MaximumPressure && CanCrushBlock(BlockVolume.Block))
                                            {
                                                WaterModComponent.Static.DamageQueue.Push(new MyTuple<IMySlimBlock, float>(BlockVolume.Block, ClosestWater.CrushDamage * recalculateFrequency));

                                                if (SimulateEffects && MyUtils.GetRandomInt(0, 200) == 0)
                                                    WaterModComponent.Static.CreateBubble(ref pointWorldPosition, Grid.GridSize);
                                            }

                                            if (SimulatePhysics)
                                            {
                                                double radius = (pointWorldPosition - _centerOfMass).LengthSquared();
                                                maxRadius = Math.Max(maxRadius, radius); //For Calculating Drag
                                                double pointDisplacement = BlockVolume.Volume * Math.Max(Math.Min(-pointDepth / Grid.GridSizeHalf, 1) * BlockVolume.Block.BuildLevelRatio, 0);
                                                
                                                if (BlockVolume.Block.FatBlock != null && !BlockVolume.Block.FatBlock.MarkedForClose)
                                                {
                                                    if (BlockVolume.Block.FatBlock is IMyGasTank)
                                                        pointDisplacement *= (1f - (BlockVolume.Block.FatBlock as IMyGasTank).FilledRatio);
                                                }

                                                Interlocked.Increment(ref _blocksUnderwater);
                                                lock (BuoyancyLock)
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

                                        BlockVolume.CurrentlyUnderwater = pointUnderwater;

                                        if (BlockVolume.PreviousUnderwater != pointUnderwater)
                                        {
                                            BlockVolume.PreviousUnderwater = pointUnderwater;

                                            if (SimulatePhysics)
                                            {
                                                if (pointUnderwater) //Enters water Enter Water
                                                {
                                                    if (BlockVolume.Block.FatBlock != null && (BlockVolume.Block.FatBlock is IMyFunctionalBlock))
                                                    {
                                                        if (!BlockVolume.Config.FunctionalUnderWater || (FluidPressure > BlockVolume.Config.MaxFunctionalPressure))
                                                        {
                                                            if (BlockVolume.Block.FatBlock is IMyThrust)
                                                            {
                                                                if (BlockVolume.Config.FunctionalUnderWater)
                                                                {
                                                                    (BlockVolume.Block.FatBlock as IMyThrust).ThrustMultiplier = 1;
                                                                }
                                                                else
                                                                {
                                                                    (BlockVolume.Block.FatBlock as IMyThrust).ThrustMultiplier = 0;
                                                                    (BlockVolume.Block.FatBlock as MyThrust).CurrentStrength = 0;
                                                                }
                                                            }
                                                            else if ((BlockVolume.Block.FatBlock as IMyFunctionalBlock).Enabled)
                                                            {
                                                                BlockVolume.CorrectState = (BlockVolume.Block.FatBlock as IMyFunctionalBlock).Enabled;
                                                                (BlockVolume.Block.FatBlock as IMyFunctionalBlock).Enabled = false;
                                                            }
                                                        }
                                                        else if (BlockVolume.CorrectState != null)
                                                        {
                                                            (BlockVolume.Block.FatBlock as IMyFunctionalBlock).Enabled = BlockVolume.CorrectState.Value;
                                                            BlockVolume.CorrectState = null;
                                                        }
                                                    }

                                                    if (Grid.BlocksDestructionEnabled && Grid.GridGeneralDamageModifier != 0)
                                                    {
                                                        Vector3 BlockVelocity = Grid.Physics.GetVelocityAtPoint(pointWorldPosition);
                                                        Vector3 BlockVerticalVelocity = Vector3.ProjectOnVector(ref BlockVelocity, ref gravityDirection);

                                                        float BlockVerticalSpeed = BlockVerticalVelocity.Length();


                                                        if (SimulateEffects)
                                                        {
                                                            if (_material.DrawSplashes && BlockVerticalSpeed > 5)
                                                            {
                                                                lock (WaterModComponent.Static.effectLock)
                                                                    WaterModComponent.Static.SurfaceSplashes.Add(new Splash(pointWorldPosition, IGrid.GridSize * (BlockVerticalSpeed / 5f)));
                                                            }
                                                        }

                                                        if (BlockVerticalVelocity.IsValid() && BlockVerticalSpeed > WaterData.GridImpactDamageSpeed / WaterDensityMultiplier)
                                                        {
                                                            WaterModComponent.Static.DamageQueue.Push(new MyTuple<IMySlimBlock, float>(BlockVolume.Block, 50 * (BlockVerticalSpeed) * WaterDensityMultiplier));
                                                        }
                                                    }
                                                }
                                                else //Exits Water Exit Water
                                                {
                                                    if (BlockVolume.Block.FatBlock != null && (BlockVolume.Block.FatBlock is IMyFunctionalBlock))
                                                    {
                                                        if (!BlockVolume.Config.FunctionalAboveWater)
                                                        {
                                                            if (BlockVolume.Block.FatBlock is IMyThrust)
                                                            {
                                                                if (BlockVolume.Config.FunctionalAboveWater)
                                                                {
                                                                    (BlockVolume.Block.FatBlock as IMyThrust).ThrustMultiplier = 1;
                                                                }
                                                                else
                                                                {
                                                                    (BlockVolume.Block.FatBlock as IMyThrust).ThrustMultiplier = 0;
                                                                    (BlockVolume.Block.FatBlock as MyThrust).CurrentStrength = 0;
                                                                }
                                                            }
                                                            else if ((BlockVolume.Block.FatBlock as IMyFunctionalBlock).Enabled)
                                                            {
                                                                BlockVolume.CorrectState = (BlockVolume.Block.FatBlock as IMyFunctionalBlock).Enabled;
                                                                (BlockVolume.Block.FatBlock as IMyFunctionalBlock).Enabled = false;
                                                            }
                                                        }
                                                        else if (BlockVolume.CorrectState != null)
                                                        {
                                                            (BlockVolume.Block.FatBlock as IMyFunctionalBlock).Enabled = BlockVolume.CorrectState.Value;
                                                            BlockVolume.CorrectState = null;
                                                        }
                                                    }

                                                    if (SimulateEffects && _material.DrawSplashes)
                                                    {
                                                        Vector3 BlockVelocity = Grid.Physics.GetVelocityAtPoint(pointWorldPosition);
                                                        Vector3 BlockVerticalVelocity = Vector3.ProjectOnVector(ref BlockVelocity, ref gravityDirection);

                                                        if (BlockVerticalVelocity.LengthSquared() < 25)
                                                            return;

                                                        lock (WaterModComponent.Static.effectLock)
                                                        {
                                                            WaterModComponent.Static.SurfaceSplashes.Add(new Splash(pointWorldPosition, IGrid.GridSize * (speed / 5f)));
                                                            WaterModComponent.Static.SimulatedSplashes.Add(new SimulatedSplash(pointWorldPosition, BlockVelocity + (MyUtils.GetRandomVector3Normalized() * 3f), IGrid.GridSize * 1.5f, ClosestWater));
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                });
                            }

                            if (SimulatePhysics)
                            {
                                if (UseAirtightness && _airtightBlocks != null && _airtightBlocks.Count > 0)
                                {
                                    float VolumeMultiplier = (Grid.GridSize * Grid.GridSize * Grid.GridSize) * ((Grid.GridSizeEnum == MyCubeSize.Large) ? WaterData.AirtightnessCoefficientLarge : WaterData.AirtightnessCoefficientSmall);
                                    float sqrGridSizeHalf = Grid.GridSizeHalf * Grid.GridSizeHalf;

                                    MyAPIGateway.Parallel.ForEach(_airtightBlocks, AirtightBlock =>
                                    {
                                        Vector3D PointWorldPosition = Grid.GridIntegerToWorld(AirtightBlock);
                                        double PointDepth = ClosestWater.GetDepthSquared(ref PointWorldPosition);
                                        
                                        if (PointDepth < sqrGridSizeHalf)
                                        {
                                            double PointDisplacement = VolumeMultiplier * Math.Max(Math.Min(-PointDepth / sqrGridSizeHalf, 1), 0);

                                            lock (BuoyancyLock)
                                            {
                                                displacement += PointDisplacement;
                                                CenterOfBuoyancy += (Vector3D)AirtightBlock * PointDisplacement;
                                            }
                                        }
                                    });
                                }

                                if (displacement > 0)
                                {
                                    PercentUnderwater = ((float)_blocksUnderwater / _lockVolumes.Length);

                                    CenterOfBuoyancy = Grid.GridIntegerToWorld(CenterOfBuoyancy / displacement);

                                    BuoyancyForce = -Grid.Physics.Gravity * _material.Density * (float)displacement * ClosestWater.Buoyancy;

                                    maxRadius = Math.Max(maxRadius, Grid.GridSizeHalf * Grid.GridSizeHalf);

                                    _dragOptimizer = 0.5f * _material.Density * ((float)maxRadius) * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS;
                                }
                                else
                                {
                                    CenterOfBuoyancy = Vector3D.Zero;
                                    BuoyancyForce = Vector3.Zero;
                                    _blocksUnderwater = 0;
                                    _dragOptimizer = 0;
                                    PercentUnderwater = 0;
                                }

                                if (SimulateEffects)
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
                                IGrid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, BuoyancyForce, CenterOfBuoyancy, null);

                            //Drag
                            if (speed > 0.1f)
                            {
                                Vector3 VelocityDamper = gravityDirection * (Vector3.Dot(Vector3.Normalize(velocity), gravityDirection) * speed * _material.Viscosity * WaterDensityMultiplier) * PercentUnderwater;

                                //Vertical Velocity Damping
                                if (VelocityDamper.IsValid() && !Vector3.IsZero(VelocityDamper, 1e-4f))
                                    IGrid.Physics.LinearVelocity -= VelocityDamper;

                                if (WaterModComponent.Static.DragAPI.Heartbeat)
                                {
                                    var api = WaterModComponent.Static.dragAPIGetter.GetOrAdd(IGrid, DragClientAPI.DragObject.Factory);

                                    if (api != null)
                                    {
                                        api.ViscosityMultiplier = 1f + (float)(PercentUnderwater * _material.Density);
                                    }
                                }
                                else
                                {
                                    DragForce = -_dragOptimizer * (velocity * speed) * PercentUnderwater;

                                    //Linear Drag
                                    if (DragForce.IsValid() && !Vector3.IsZero(DragForce, 1e-4f))
                                        IGrid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, Vector3.ClampToSphere(DragForce, Entity.Physics.Mass * speed), null, null);
                                }
                            }

                            //Angular Drag
                            if (angularSpeed > 0.03f)
                            {
                                //https://web.archive.org/web/20160506233549/http://www.randygaul.net/wp-content/uploads/2014/02/RigidBodies_WaterSurface.pdf
                                //Td = Ba*m*(V/Vb)*L^2*w
                                //TorqueDrag = DragCoeff * mass * (VolumeWater / VolumeBody) * Length^2 * AngularVelocity
                                Vector3 AngularDragForce = ((MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS / 1000000000f) * Entity.Physics.Mass * PercentUnderwater * (float)maxRadius) * IGrid.Physics.AngularVelocity;

                                if (AngularDragForce.IsValid() && !Vector3.IsZero(AngularDragForce, 1e-4f))
                                    IGrid.Physics.AngularVelocity -= Vector3.ClampToSphere(AngularDragForce, angularSpeed);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (!MyAPIGateway.Utilities.IsDedicated)
                    MyAPIGateway.Utilities.ShowNotification("Water Error on Grid. Phys-" + SimulatePhysics + "Efct-" + SimulateEffects, 16 * recalculateFrequency);

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

            if (Wakes == null)
            {
                Wakes = new List<AnimatedBillboard>();
            }

            for (int i = Wakes.Count - 1; i >= 0; i--)
            {
                AnimatedBillboard Splash = Wakes[i];
                if (Splash == null || Splash.MarkedForClose)
                {
                    Wakes.RemoveAtFast(i);
                    continue;
                }
                else
                {
                    Splash.Simulate();
                    Splash.Velocity *= 0.9f;
                }
            }

            if (WaterModComponent.Static.Settings.ShowCenterOfBuoyancy)
            {
                if (COBIndicators == null)
                {
                    COBIndicators = new MyBillboard[3];

                    for (int i = 0; i < 3; i++)
                    {
                        COBIndicators[i] = new MyBillboard()
                        {
                            Color = WaterData.RedColor,
                            ColorIntensity = 1,
                            CustomViewProjection = -1,
                            BlendType = MyBillboard.BlendTypeEnum.AdditiveTop,
                            Material = WaterData.BlankMaterial,
                            UVOffset = Vector2.Zero,
                            UVSize = Vector2.One,
                        };
                        MyTransparentGeometry.AddBillboard(COBIndicators[i], true);
                    }
                }
                else
                {
                    MyBillboard Billboard;
                    Vector3D SideVector;
                    Vector3D CameraNormal = MyUtils.Normalize(WaterModComponent.Static.Session.CameraPosition - CenterOfBuoyancy);
                    Vector3D Direction;
                    Vector3D Offset;

                    //Forward
                    Billboard = COBIndicators[0];
                    Direction = Grid.WorldMatrix.Forward;
                    Offset = Direction * Grid.GridSizeHalf;
                    SideVector = MyUtils.GetVector3Scaled(Vector3D.Cross(Direction, CameraNormal), 0.01f);
                    Billboard.Position0 = CenterOfBuoyancy - Offset - SideVector;
                    Billboard.Position1 = CenterOfBuoyancy + Offset - SideVector;
                    Billboard.Position2 = CenterOfBuoyancy + Offset + SideVector;
                    Billboard.Position3 = CenterOfBuoyancy - Offset + SideVector;

                    //Right
                    Billboard = COBIndicators[1];
                    Direction = Grid.WorldMatrix.Right;
                    Offset = Direction * Grid.GridSizeHalf;
                    SideVector = MyUtils.GetVector3Scaled(Vector3D.Cross(Direction, CameraNormal), 0.01f);
                    Billboard.Position0 = CenterOfBuoyancy - Offset - SideVector;
                    Billboard.Position1 = CenterOfBuoyancy + Offset - SideVector;
                    Billboard.Position2 = CenterOfBuoyancy + Offset + SideVector;
                    Billboard.Position3 = CenterOfBuoyancy - Offset + SideVector;

                    //Up
                    Billboard = COBIndicators[2];
                    Direction = Grid.WorldMatrix.Up;
                    Offset = Direction * Grid.GridSizeHalf;
                    SideVector = MyUtils.GetVector3Scaled(Vector3D.Cross(Direction, CameraNormal), 0.01f);
                    Billboard.Position0 = CenterOfBuoyancy - Offset - SideVector;
                    Billboard.Position1 = CenterOfBuoyancy + Offset - SideVector;
                    Billboard.Position2 = CenterOfBuoyancy + Offset + SideVector;
                    Billboard.Position3 = CenterOfBuoyancy - Offset + SideVector;
                }
            }
            else if (COBIndicators != null)
            {
                //Cleanup billboards
                MyTransparentGeometry.RemovePersistentBillboards(COBIndicators);

                COBIndicators = null;
            }
        }

        public enum MyEntityType
        {
            Entity,
            CubeGrid,
            Character,
            FloatingObject
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
            public Vector3I Key;
            public double Volume;

            public BlockVolumeData(Vector3I key, BlockConfig config, IMySlimBlock block, bool previousUnderwater = false, bool currentlyUnderwater = false)
            {
                CurrentlyUnderwater = currentlyUnderwater;
                Key = key;
                Config = config;
                PreviousUnderwater = previousUnderwater;
                Block = block;

                if (config.IsPressurized)
                    Volume = config.Volume * (block.CubeGrid.GridSizeEnum == MyCubeSize.Large ? WaterData.AirtightnessCoefficientLarge : WaterData.AirtightnessCoefficientSmall);
                else
                    Volume = config.Volume * (block.CubeGrid.GridSizeEnum == MyCubeSize.Large ? WaterData.BuoyancyCoefficientLarge : WaterData.BuoyancyCoefficientSmall);
            }
        }
    }
}