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
        public WaterComponent ClosestWater;

        /// <summary>
        /// If this it true, it will recalculate buoyancy the next frame
        /// </summary>
        public bool NeedsRecalculateBuoyancy;

        /// <summary>
        /// Simulates the phyics if true
        /// </summary>
        public bool SimulatePhysics;

        /// <summary>
        /// Hydrostatcic Pressure of the fluid the entity is at. The Unit is kPa (KiloPascals)
        /// </summary>
        public float FluidPressure;

        /// <summary>
        /// Depth of the entity in the fluid. Unit is m (Meters) Positive is above water, negative is below water
        /// </summary>
        public double FluidDepth;

        /// <summary>
        /// Velocity of the fluid (Currents, Wave Oscillation)
        /// </summary>
        public Vector3 FluidVelocity;

        /// <summary>
        /// Unit is N (Newtons) Force vector for buoyancy
        /// </summary>
        public Vector3 BuoyancyForce;

        /// <summary>
        /// Center of buoyancy position of the entity in World Space
        /// </summary>
        public Vector3D CenterOfBuoyancy;

        /// <summary>
        /// Unit is N (Newtons) Force vector for drag
        /// </summary>
        public Vector3D DragForce;

        /// <summary>
        /// Percentage of the entity being underwater where 1 is 100%
        /// </summary>
        public float PercentUnderwater;

        protected Vector3 _gravity;
        protected Vector3 _gravityDirection;
        protected float _gravityStrength;
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
        protected WaterRenderSessionComponent _renderComponent;
        protected WaterEffectsComponent _effectsComponent;

        protected bool IsValid => Entity.Physics != null && !Entity.Transparent && !Entity.MarkedForClose;

        public WaterPhysicsComponentBase()
        {
            _modComponent = Session.Instance.Get<WaterModComponent>();
            _renderComponent = Session.Instance.TryGet<WaterRenderSessionComponent>();
            _effectsComponent = Session.Instance.TryGet<WaterEffectsComponent>();
        }

        public override void OnAddedToContainer()
        {
            if (Entity.Transparent)
                return;
            
            _modComponent.UpdateAction += UpdateAfter1;
            _modComponent.UpdateActionSparse += UpdateAfter60;
            Entity.OnPhysicsChanged += Entity_OnPhysicsChanged;

            _position = Entity.PositionComp.WorldVolume.Center;

            UpdateGravity();
        }

        public override void OnBeforeRemovedFromContainer()
        {
            _modComponent.UpdateAction -= UpdateAfter1;
            _modComponent.UpdateActionSparse -= UpdateAfter60;
            Entity.OnPhysicsChanged -= Entity_OnPhysicsChanged;
        }

        public virtual void UpdateAfter60()
        {
            if (IsValid && ClosestWater != null && Entity.Physics != null)
            {
                FluidPressure = (ClosestWater.Settings.Material.Density * _gravityStrength * (float)Math.Max(-FluidDepth, 0)) / 1000;

                if (ClosestWater.Planet?.HasAtmosphere == true)
                {
                    //(101325 / 1000) Converts atm to kPa. 1atm = 101.325kPa
                    FluidPressure += ClosestWater.Planet.GetOxygenForPosition(_position) * (101325 / 1000);
                }

                UpdateGravity();
            }
        }

        private void UpdateGravity()
        {
            _gravity = MyAPIGateway.Physics.CalculateNaturalGravityAt(_position, out _gravityStrength);
            _gravityDirection = _gravity;
            _gravityStrength = _gravityDirection.Normalize();
        }

        public virtual void UpdateAfter1()
        {
            if (!IsValid)
                return;

            _position = Entity.PositionComp.WorldVolume.Center;

            UpdateClosestWater();

            if (ClosestWater == null)
                return;

            FluidDepth = ClosestWater.GetDepthGlobal(ref _position);
            FluidVelocity = ClosestWater.GetFluidVelocityGlobal(-_gravityDirection);

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
