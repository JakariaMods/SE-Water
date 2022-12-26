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
    public class WaterPhysicsComponentInventoryBag : WaterPhysicsComponentBase
    {
        private float _dragOptimizer;
        private double _maxRadius;

        public IMyInventoryBag InventoryBag;
        private bool _airtight;
        private bool _underwater;

        public WaterPhysicsComponentInventoryBag() : base() { }

        /// <summary>
        /// Initalize the component
        /// </summary>
        public override void OnAddedToContainer()
        {
            InventoryBag = Entity as IMyInventoryBag;

            base.OnAddedToContainer();

            NeedsRecalculateBuoyancy = true;
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

            if (Entity.Physics == null || ClosestWater == null || ClosestWater.Settings.Material == null)
                return;

            if (SimulatePhysics)
            {
                if (ClosestWater == null || !Entity.InScene || Entity.Physics == null || Entity.MarkedForClose || _gravityStrength == 0 && Entity.Physics.Mass == 0)
                    return;

                if (!_airtight && FluidDepth < 0)
                {
                    _nextRecalculate--;
                    if (NeedsRecalculateBuoyancy || _nextRecalculate <= 0)
                    {
                        _nextRecalculate = WaterData.BuoyancyUpdateFrequencyEntity;
                        NeedsRecalculateBuoyancy = false;
                        PercentUnderwater = (float)Math.Max(Math.Min(-FluidDepth / _maxRadius, 1), 0);
                        BuoyancyForce = -_gravity * ClosestWater.Settings.Material.Density * (WaterData.SphereVolumeOptimizer * (float)(_maxRadius * _maxRadius)) * PercentUnderwater * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS * ClosestWater.Settings.Buoyancy;
                        _dragOptimizer = 0.5f * ClosestWater.Settings.Material.Density * (float)(_maxRadius * _maxRadius) * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS;
                    }

                    if (_speed > 0.1f)
                    {
                        Vector3 VelocityDamper = _gravityDirection * (Vector3.Dot(Vector3.Normalize(Entity.Physics.LinearVelocity), _gravityDirection) * _speed * ClosestWater.Settings.Material.Viscosity);

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
                    if (_effectsComponent != null && ClosestWater.Settings.Material.DrawSplashes)
                    {
                        bool underwater = FluidDepth <= _maxRadius;

                        if (_underwater != underwater)
                        {
                            _underwater = underwater;

                            _effectsComponent.CreateSplash(_position, Math.Min(_speed, 2f), true);
                        }
                    }
                }
            }
        }
    }
}