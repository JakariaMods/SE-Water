using Jakaria.Utils;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using Jakaria.SessionComponents;

namespace Jakaria.Components
{
    public class WaterPhysicsComponentFloatingObject : WaterPhysicsComponentBase
    {
        private float _dragOptimizer = 0;
        private double _maxRadius;
        private bool _airtight;

        public MyFloatingObject FloatingObject;

        public WaterPhysicsComponentFloatingObject(WaterManagerComponent managerComponent) : base(managerComponent) { }

        /// <summary>
        /// Initalize the component
        /// </summary>
        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();

            NeedsRecalculateBuoyancy = true;

            FloatingObject = Entity as MyFloatingObject;
        }

        /// <summary>
        /// Secondary simulation method for updating low priority/frequency stuff like drowning
        /// </summary>
        public override void UpdateAfter60()
        {
            base.UpdateAfter60();

            if (ClosestWater != null)
            {
                _maxRadius = Entity.PositionComp.LocalVolume.Radius;

                _airtight = WaterUtils.IsPositionAirtight(ref _position);
            }
        }

        /// <summary>
        /// Simulation method for physics
        /// </summary>
        public override void UpdateAfter1()
        {
            base.UpdateAfter1();

            if (Entity.Physics == null || ClosestWater == null || ClosestWater.Material == null)
                return;

            try
            {
                if (SimulatePhysics || SimulateEffects)
                {
                    if (ClosestWater == null || !Entity.InScene || Entity.Physics == null || Entity.MarkedForClose || _gravity == 0 && Entity.Physics.Mass == 0)
                        return;

                    if (!_airtight && FluidDepth < 0)
                    {
                        if (FloatingObject.Item.Content?.SubtypeId != null && ClosestWater.Material?.CollectedItem?.SubtypeId != null)
                        {
                            if (FloatingObject.Item.Content.SubtypeId == ClosestWater.Material.CollectedItem.SubtypeId)
                            {
                                Entity.Close();
                                return;
                            }
                        }

                        _nextRecalculate--;
                        if (NeedsRecalculateBuoyancy || _nextRecalculate <= 0)
                        {
                            _nextRecalculate = WaterData.BuoyancyUpdateFrequencyEntity;
                            NeedsRecalculateBuoyancy = false;
                            PercentUnderwater = (float)Math.Max(Math.Min(-FluidDepth / _maxRadius, 1), 0);
                            BuoyancyForce = -Entity.Physics.Gravity * ClosestWater.Material.Density * (WaterData.SphereVolumeOptimizer * (float)(_maxRadius * _maxRadius)) * PercentUnderwater * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS * ClosestWater.Buoyancy;
                            _dragOptimizer = 0.5f * ClosestWater.Material.Density * (float)(_maxRadius * _maxRadius) * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS;

                            if (Entity is MyMeteor)
                            {
                                MyVisualScriptLogicProvider.CreateExplosion(_position, 2, 0);
                                MyFloatingObjects.Spawn(new MyPhysicalInventoryItem(500 * (MyFixedPoint)MyUtils.GetRandomFloat(1f, 3f), MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Ore>(WaterUtils.GetRandomMeteorMaterial())), Entity.WorldMatrix, null, null);
                                Entity.Close();
                                return;
                            }
                        }

                        if (_speed > 0.1f)
                        {
                            Vector3 VelocityDamper = _gravityDirection * (Vector3.Dot(Vector3.Normalize(Entity.Physics.LinearVelocity), _gravityDirection) * _speed * ClosestWater.Material.Viscosity);

                            //Vertical Velocity Damping
                            if (VelocityDamper.IsValid() && !Vector3.IsZero(VelocityDamper, 1e-4f))
                                Entity.Physics.LinearVelocity -= VelocityDamper;

                            DragForce = -_dragOptimizer * (_speed * _velocity);

                            //Linear Drag
                            if (DragForce.IsValid() && !Vector3.IsZero(DragForce, 1e-4f))
                                Entity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, Vector3.ClampToSphere(DragForce, Entity.Physics.Mass * _speed), null, null);
                        }

                        //Angular Drag
                        if (_angularSpeed > 0.03f)
                        {
                            Vector3 AngularDragForce = ((PercentUnderwater * _dragOptimizer * _angularSpeed) * Entity.Physics.AngularVelocity) / Entity.Physics.Mass;

                            if (AngularDragForce.IsValid() && !Vector3.IsZero(AngularDragForce, 1e-4f))
                                Entity.Physics.AngularVelocity -= Vector3.ClampToSphere(AngularDragForce, _angularSpeed / 2);
                        }

                        if (BuoyancyForce.IsValid())
                            Entity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, BuoyancyForce, null, null);

                        //Simple Splash Effects
                        if (SimulateEffects && ClosestWater.Material.DrawSplashes)
                        {
                            if (FluidDepth < 0)
                            {
                                if (SimulateEffects && FluidDepth > -1 && FluidDepth < 1 && _verticalSpeed > 2f)
                                {
                                    _effectsComponent.CreateSplash(_position, Math.Min(_speed, 2f), true);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (!MyAPIGateway.Utilities.IsDedicated)
                    MyAPIGateway.Utilities.ShowNotification("Water Error on Floating Object. Phys-" + SimulatePhysics + "Efct-" + SimulateEffects, 16 * _recalculateFrequency);

                WaterUtils.WriteLog(e.ToString());
            }
        }
    }
}