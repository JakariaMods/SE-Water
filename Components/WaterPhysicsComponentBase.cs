using Jakaria.Utils;
using Sandbox.ModAPI;
using System;
using VRage.Game.Components;
using VRage.ModAPI;
using VRageMath;
using Jakaria.SessionComponents;

namespace Jakaria.Components
{
    public class WaterPhysicsComponentBase : MyGameLogicComponent
    {
        /// <summary>
        /// The Water that the entity will use to simulate with, can be null
        /// </summary>
        public Water ClosestWater { get; protected set; }

        /// <summary>
        /// If this it true, it will recalculate buoyancy the next frame
        /// </summary>
        public bool NeedsRecalculateBuoyancy;

        /// <summary>
        /// Simulates client-effects if true
        /// </summary>
        public bool SimulateEffects;

        /// <summary>
        /// Simulates the phyics if true
        /// </summary>
        public bool SimulatePhysics;

        /// <summary>
        /// Hydrostatcic Pressure of the fluid the entity is at. The Unit is kPa (KiloPascals)
        /// </summary>
        public float FluidPressure { get; protected set; }

        /// <summary>
        /// Depth of the entity in the fluid. Unit is m (Meters) Positive is above water, negative is below water
        /// </summary>
        public double FluidDepth { get; protected set; }

        /// <summary>
        /// Velocity of the fluid (Currents, Wave Oscillation)
        /// </summary>
        public Vector3 FluidVelocity { get; protected set; }

        /// <summary>
        /// Unit is N (Newtons) Force vector for buoyancy
        /// </summary>
        public Vector3 BuoyancyForce { get; protected set; }

        /// <summary>
        /// Center of buoyancy position of the entity in World Space
        /// </summary>
        public Vector3D CenterOfBuoyancy { get; protected set; }

        /// <summary>
        /// Unit is N (Newtons) Force vector for drag
        /// </summary>
        public Vector3 DragForce { get; protected set; }

        /// <summary>
        /// Percentage of the entity being underwater where 1 is 100%
        /// </summary>
        public float PercentUnderwater { get; protected set; }

        protected Vector3 _gravityDirection;
        protected float _gravity;
        protected Vector3D _position;

        protected Vector3 _velocity;
        protected Vector3 _velocityDirection;
        protected float _speed;
        protected Vector3 _verticalVelocity;
        protected float _verticalSpeed;
        protected float _angularSpeed;

        protected int _nextRecalculate = 0;
        protected int _recalculateFrequency = 0;

        protected WaterModComponent _modComponent;
        protected WaterRenderComponent _renderComponent;
        protected WaterEffectsComponent _effectsComponent;

        public WaterPhysicsComponentBase(WaterManagerComponent managerComponent)
        {
            _modComponent = managerComponent.TryGet<WaterModComponent>();
            _renderComponent = managerComponent.TryGet<WaterRenderComponent>();
            _effectsComponent = managerComponent.TryGet<WaterEffectsComponent>();
        }

        public override void OnAddedToContainer()
        {
            _modComponent.UpdateAfter1 += UpdateAfter1;
            _modComponent.UpdateAfter60 += UpdateAfter60;
            Entity.OnPhysicsChanged += Entity_OnPhysicsChanged;

            UpdateClosestWater();
        }

        public override void OnBeforeRemovedFromContainer()
        {
            _modComponent.UpdateAfter1 -= UpdateAfter1;
            _modComponent.UpdateAfter60 -= UpdateAfter60;
            Entity.OnPhysicsChanged -= Entity_OnPhysicsChanged;
        }

        public virtual void UpdateAfter60()
        {
            UpdateClosestWater();

            if (_renderComponent != null && Vector3D.DistanceSquared(_position, _renderComponent.CameraPosition) < MyAPIGateway.Session.SessionSettings.SyncDistance * MyAPIGateway.Session.SessionSettings.SyncDistance)
            {
                SimulateEffects = true;
            }
            else
                SimulateEffects = false;

            if(ClosestWater != null && Entity.Physics != null)
            {
                FluidPressure = (ClosestWater.Material.Density * _gravity * (float)Math.Max(-FluidDepth, 0)) / 1000;

                if (ClosestWater.Planet?.HasAtmosphere == true)
                {
                    //(101325 / 1000) Converts atm to kPa. 1atm = 101.325kPa
                    FluidPressure += ClosestWater.Planet.GetOxygenForPosition(_position) * (101325 / 1000);
                }

                _gravityDirection = Entity.Physics.Gravity;
                _gravity = _gravityDirection.Normalize();
            }
        }

        public virtual void UpdateAfter1()
        {
            if (Entity.Physics == null || ClosestWater == null || ClosestWater.Material == null)
                return;

            if(SimulatePhysics || SimulateEffects)
            {
                _position = Entity.PositionComp.WorldVolume.Center;

                FluidDepth = ClosestWater.GetDepth(ref _position);
                FluidVelocity = ClosestWater.GetFluidVelocity(-_gravityDirection);

                if (SimulatePhysics)
                {
                    _velocity = Entity.Physics.LinearVelocity - FluidVelocity;
                    _velocityDirection = _velocity;
                    _speed = _velocityDirection.Normalize();

                    _verticalVelocity = Vector3.ProjectOnVector(ref _velocity, ref _gravityDirection);
                    _verticalSpeed = _verticalVelocity.Length();

                    _angularSpeed = Entity.Physics.AngularVelocity.Length();
                }
            }
        }

        /// <summary>
        /// Updates the closest water to the entity
        /// </summary>
        protected void UpdateClosestWater()
        {
            ClosestWater = _modComponent.GetClosestWater(_position);
        }

        /// <summary>
        /// When the entity changes from static to ship or vise versa
        /// </summary>
        private void Entity_OnPhysicsChanged(IMyEntity obj)
        {
            if (obj.Physics == null)
            {
                SimulatePhysics = false;
            }
            else
            {
                if (SimulatePhysics == obj.Physics.IsStatic)
                {
                    SimulatePhysics = !obj.Physics.IsStatic;

                    OnSimulatePhysicsChanged(SimulatePhysics);
                }
            }
        }

        /// <summary>
        /// Called when the SimulatePhysics boolean changes value
        /// </summary>
        protected virtual void OnSimulatePhysicsChanged(bool value)
        {

        }
    }
}
